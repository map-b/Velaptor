﻿// <copyright file="SoundLoader.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Content
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using Velaptor.Audio;
    using Velaptor.Audio.Exceptions;
    using Velaptor.NativeInterop.OpenAL;

    /// <summary>
    /// Loads sound content.
    /// </summary>
    public class SoundLoader : ILoader<ISound>
    {
        private readonly ConcurrentDictionary<string, ISound> sounds = new ();
        private readonly IALInvoker alInvoker;
        private readonly IAudioDeviceManager audioManager;
        private readonly IPathResolver soundPathResolver;
        private readonly ISoundDecoder<float> oggDecoder;
        private readonly ISoundDecoder<byte> mp3Decoder;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundLoader"/> class.
        /// </summary>
        /// <param name="soundPathResolver">Resolves the path to the sound content.</param>
        /// <param name="oggDecoder">Decodes ogg sound files.</param>
        /// <param name="mp3Decoder">Decodes mp3 sound files.</param>
        // TODO: The decoders were turned into internals.  This had to be commented out 
        // because it would expose those internals because this class is public and has
        // to stay there.  With no public constructor, the user could not create a new instance
        // of this class.  To allow this, refactor this CTOR to internally in the CTOR code
        // to use the IoC container to load up the intenal types
        //[ExcludeFromCodeCoverage]
        //public SoundLoader(IPathResolver soundPathResolver, ISoundDecoder<float> oggDecoder, ISoundDecoder<byte> mp3Decoder)
        //{
        //    this.soundPathResolver = soundPathResolver;
        //    this.oggDecoder = oggDecoder;
        //    this.mp3Decoder = mp3Decoder;
        //    this.alInvoker = new OpenTKALInvoker();
        //    this.audioManager = IoC.Container.GetInstance<IAudioDeviceManager>();
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundLoader"/> class.
        /// </summary>
        /// <param name="alInvoker">Makes calls to OpenAL.</param>
        /// <param name="audioManager">Manages the audio devices.</param>
        /// <param name="soundPathResolver">Resolves paths to sound content.</param>
        /// <param name="oggDecoder">Decodes ogg sound files.</param>
        /// <param name="mp3Decoder">Decodes mp3 sound files.</param>
        internal SoundLoader(
            IALInvoker alInvoker,
            IAudioDeviceManager audioManager,
            IPathResolver soundPathResolver,
            ISoundDecoder<float> oggDecoder,
            ISoundDecoder<byte> mp3Decoder)
        {
            this.alInvoker = alInvoker;
            this.audioManager = audioManager;
            this.soundPathResolver = soundPathResolver;
            this.oggDecoder = oggDecoder;
            this.mp3Decoder = mp3Decoder;
        }

        /// <summary>
        /// Loads a sound with the given name.
        /// </summary>
        /// <param name="name">The name of the sound content to load.</param>
        /// <returns>The name of the sound content item to load.</returns>
        /// <remarks>
        ///     The content <see cref="name"/> can have or not have the extension of the content file to load.
        ///     Using the file extension has no effect and the content item is matched using the name without the extension.
        ///     The only sounds supported are .ogg and .mp3 files.
        ///     Also, duplicate names are not aloud in the same content source and this is matched against the name without the extension.
        ///
        ///     Example: sound.ogg and sound.mp3 will result in an exception.
        /// </remarks>
        /// <exception cref="UnsupportedSoundTypeException">
        ///     Thrown when a sound file extension other then '.ogg' and '.mp3' are used.
        /// </exception>
        public ISound Load(string name)
        {
            var filePath = this.soundPathResolver.ResolveFilePath(name);

            return this.sounds.GetOrAdd(filePath, (key) =>
            {
                return new Sound(
                    key,
                    this.alInvoker,
                    this.audioManager,
                    this.oggDecoder,
                    this.mp3Decoder);
            });
        }

        /// <inheritdoc/>
        public void Unload(string name)
        {
            var filePath = this.soundPathResolver.ResolveFilePath(name);

            if (this.sounds.TryRemove(filePath, out var sound))
            {
                sound.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to dispose of managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var sound in this.sounds.Values)
                {
                    sound.Dispose();
                }

                this.audioManager.Dispose();
            }

            this.isDisposed = true;
        }
    }
}