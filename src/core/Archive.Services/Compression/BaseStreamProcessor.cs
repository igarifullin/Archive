using System;
using System.IO;
using System.Linq;
using System.Threading;
using Archive.Data;
using Archive.Data.Collections;
using Archive.Data.Exceptions;
using static Archive.Utils.ExceptionWrapper;

namespace Archive.Services.Compression
{
    /// <summary>
    /// Provides stream processor with parallel handling.
    /// </summary>
    public abstract class BaseStreamProcessor : IDisposable
    {
        private readonly object _lockObj = new object();

        protected CompressionParams _params;
        private CountdownEvent _countdownEvent;
        private SafeIndexQueue<byte[]> _inputQueue;
        private SafeIndexQueue<byte[]> _outputQueue;

        private Thread _readThread;
        private Thread _writeThread;
        private Thread[] _processThreads;
        
        private bool _isDisposed;

        public void Execute(CompressionParams @params, CancellationToken cancellationToken)
        {
            lock (_lockObj)
            {
                CheckDisposed();
                
                Validate(@params);

                Init(@params, cancellationToken);
                
                StartReadInBackground();

                StartProcessInBackground();

                StartWriteInBackground();

                WaitWriteComplete();
            }
        }


        private void Init(CompressionParams @params, CancellationToken cancellationToken)
        {
            _params = @params;

            _readThread = new Thread(() => Wrap<ReadException>(() => ReadInternal(_params.InputPath, cancellationToken)));
            _readThread.IsBackground = true;

            _writeThread = new Thread(() => Wrap<WriteException>(() => WriteInternal(_params.OutputPath, cancellationToken)));
            _writeThread.IsBackground = true;

            var queueLimit = Environment.ProcessorCount;

            // set limit for queues
            _inputQueue = new SafeIndexQueue<byte[]>(queueLimit * 2);
            _outputQueue = new SafeIndexQueue<byte[]>(queueLimit * 2);

            var processThreadsCount = GetOptimalThreadsCount();

            _countdownEvent = new CountdownEvent(processThreadsCount);
            _processThreads = new Thread[processThreadsCount];
            for (var i = 0; i < processThreadsCount; i++)
            {
                _processThreads[i] = new Thread(() => Wrap<ProcessException>(() => ProcessInternal(cancellationToken)));
                _processThreads[i].IsBackground = true;
            }
        }

        private void Validate(CompressionParams @params)
        {
            if (!File.Exists(@params.InputPath))
                throw new FileNotFoundException("Source file should exists");

            if (!File.Exists(@params.OutputPath))
                return;

            try
            {
                File.Delete(@params.OutputPath);
            }
            catch (Exception e)
            {
                throw new ValidationException("Destination file exists and couldn't be deleted", e);
            }
        }

        private int GetOptimalThreadsCount()
        {
            var processThreadsCount = 1;
            if (Environment.ProcessorCount > 2)
            {
                processThreadsCount = Environment.ProcessorCount - 2;
            }

            return processThreadsCount;
        }

        private void StartReadInBackground()
        {
            _readThread.Start();
        }

        private void ReadInternal(string inputPath, CancellationToken cancellationToken)
        {
            using (var stream = File.OpenRead(inputPath))
            {
                var idx = 0;
                while (stream.CanRead)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var block = Read(stream);
                    if (block == null)
                    {
                        _inputQueue.CompleteAdding();
                        break;
                    }

                    _inputQueue.Enqueue(new IndexedQueueElement<byte[]>(idx, block));
                    idx++;
                }
            }
        }

        protected virtual byte[] Read(Stream stream)
        {
            var block = new byte[_params.BlockSize];
            var bytesRead = stream.Read(block, 0, _params.BlockSize);

            block = block.Take(bytesRead).ToArray();

            return bytesRead > 0
                ? block
                : null;
        }

        private void StartProcessInBackground()
        {
            foreach (var thread in _processThreads)
            {
                thread.Start();
            }
        }

        private void ProcessInternal(CancellationToken cancellationToken)
        {
            // Process items until queue is not completed for adding.
            while (!_inputQueue.IsCompleted)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var element = _inputQueue.Dequeue();
                if (element == null)
                    continue;

                var processedBlock = Process(element.Value);
                _outputQueue.Enqueue(new IndexedQueueElement<byte[]>(element.Index, processedBlock));
            }

            _countdownEvent.Signal();

            // Wait until all threads signal the end of processing.
            _countdownEvent.Wait(cancellationToken);

            // Mark output queue as completed for adding.
            if (!_outputQueue.IsAddingCompleted)
                _outputQueue.CompleteAdding();
        }

        protected abstract byte[] Process(byte[] block);        

        private void StartWriteInBackground()
        {
            _writeThread.Start();
        }

        private void WriteInternal(string outputPath, CancellationToken cancellationToken)
        {
            using (var stream = File.OpenWrite(outputPath))
            {
                do
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    byte[] block = _outputQueue.Dequeue()?.Value;

                    if (block != null)
                        Write(stream, block);
                }
                // Process items until queue is not completed for adding.
                while (!_outputQueue.IsCompleted);
            }

            // Delete output file, if cancellation requested.
            if (cancellationToken.IsCancellationRequested)
            {
                File.Delete(_params.OutputPath);
            }
        }

        protected virtual void Write(Stream stream, byte[] block)
        {
            stream.Write(block, 0, block.Length);
        }

        private void WaitWriteComplete()
        {
            _writeThread.Join();
        }

        private void CancelInternal()
        {
            if (_readThread.IsAlive)
                _readThread.Join();

            CancelProcessing();

            if (_writeThread.IsAlive)
                _writeThread.Join();

            _inputQueue?.Clear();
            _inputQueue?.Dispose();

            _outputQueue?.Clear();
            _outputQueue?.Dispose();
        }

        private void CancelProcessing()
        {
            foreach (var thread in _processThreads)
            {
                if (thread.IsAlive)
                    thread.Join();
            }
        }

        public void Dispose()
        {
            
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                CancelInternal();
                _isDisposed = true;
            }
        }
        
        /// <summary>Throws a System.ObjectDisposedException if the collection was disposed</summary>
        /// <exception cref="System.ObjectDisposedException">If the collection has been disposed.</exception>
        protected void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(BaseStreamProcessor));
            }
        }
    }
}