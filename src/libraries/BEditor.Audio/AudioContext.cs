﻿using System;
using System.Numerics;

using OpenTK.Audio.OpenAL;

namespace BEditor.Audio
{
    /// <summary>
    /// Represents an OpenAL context.
    /// </summary>
    public sealed class AudioContext : IDisposable
    {
        private readonly ALDevice _device;
        private readonly ALContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="AudioContext"/> class.
        /// </summary>
        public unsafe AudioContext()
        {
            _device = ALC.OpenDevice(null);
            _context = ALC.CreateContext(_device, (int[])null!);

            var alcError = ALC.GetError(_device);

            if (alcError is not AlcError.NoError)
            {
                throw new AudioException(alcError.ToString("g"));
            }

            MakeCurrent();
        }

        /// <summary>
        /// Get whether an object has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Get whether this context is current or not.
        /// </summary>
        public bool IsCurrent => ALC.GetCurrentContext() == _context;

        /// <summary>
        /// Gets or sets the applied gain (volume amplification).
        /// </summary>
        /// <exception cref="ObjectDisposedException">This object has been discarded.</exception>
        /// <exception cref="AudioException">An error has occurred in OpenAL.</exception>
        public float Gain
        {
            get
            {
                ThrowIfDisposed();
                MakeCurrent();
                AL.GetListener(ALListenerf.Gain, out var v);

                CheckError();

                return v;
            }
            set
            {
                ThrowIfDisposed();
                MakeCurrent();
                AL.Listener(ALListenerf.Gain, value);

                CheckError();
            }
        }

        internal void CheckError()
        {
            var error = AL.GetError();

            if (error is not ALError.NoError)
            {
                throw new AudioException(AL.GetErrorString(error));
            }

            var alcError = ALC.GetError(_device);

            if (alcError is not AlcError.NoError)
            {
                throw new AudioException(alcError.ToString("g"));
            }
        }
        private void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
        /// <summary>
        /// Set this context to current.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This object has been discarded.</exception>
        /// <exception cref="AudioException">An error has occurred in OpenAL.</exception>
        public void MakeCurrent()
        {
            ThrowIfDisposed();

            if (!IsCurrent)
            {
                ALC.MakeContextCurrent(_context);

                CheckError();
            }
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            if (IsDisposed) return;

            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(_context);
            ALC.CloseDevice(_device);
            GC.SuppressFinalize(this);

            IsDisposed = true;
        }
    }
}