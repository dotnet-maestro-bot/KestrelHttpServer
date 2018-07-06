// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2OutputProducer : IHttpOutputProducer
    {
        private readonly int _streamId;
        private readonly IHttp2FrameWriter _frameWriter;
        private readonly IKestrelTrace _kestrelTrace;
        private int _requestAborted;
        private bool _completed;

        public Http2OutputProducer(int streamId, IHttp2FrameWriter frameWriter, IKestrelTrace kestrelTrace)
        {
            _streamId = streamId;
            _frameWriter = frameWriter;
            _kestrelTrace = kestrelTrace;
        }

        private bool RequestAborted => _requestAborted == 1;

        public bool IsCompleted => _completed || RequestAborted;

        public void Dispose()
        {
        }

        public void Abort(ConnectionAbortedException error)
        {
            if (Interlocked.Exchange(ref _requestAborted, 1) != 0)
            {
                return;
            }
        }

        public Task WriteAsync<T>(Func<PipeWriter, T, long> callback, T state)
        {
            throw new NotImplementedException();
        }

        public Task FlushAsync(CancellationToken cancellationToken) => _frameWriter.FlushAsync(cancellationToken);

        public Task Write100ContinueAsync(CancellationToken cancellationToken)
        {
            if (RequestAborted)
            {
                // TODO: Log trace - 100 continue suppressed for aborted stream.
                return Task.CompletedTask;
            }
            return _frameWriter.Write100ContinueAsync(_streamId);
        }

        public Task WriteDataAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken)
        {
            if (RequestAborted)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    return Task.FromException(new IOException("The response stream has been aborted."));
                }
                // TODO: Log trace - Response data suppressed for aborted stream.
                return Task.CompletedTask;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return _frameWriter.WriteDataAsync(_streamId, data, cancellationToken);
        }

        public Task WriteStreamSuffixAsync(CancellationToken cancellationToken)
        {
            if (RequestAborted)
            {
                // TODO: Log Trace - Response suffix suppressed for aborted stream.
                return Task.CompletedTask;
            }
            _completed = true;
            return _frameWriter.WriteDataAsync(_streamId, Constants.EmptyData, endStream: true, cancellationToken: cancellationToken);
        }

        public void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders)
        {
            if (RequestAborted)
            {
                // TODO: Log Debug - Response headers suppressed for aborted stream.
            }
            else
            {
                // The HPACK header compressor is stateful, if we compress headers for an aborted stream we must send them.
                // Optimize for not compressing or sending them.
                _frameWriter.WriteResponseHeaders(_streamId, statusCode, responseHeaders);
            }
        }
    }
}
