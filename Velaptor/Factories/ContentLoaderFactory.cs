﻿// <copyright file="ContentLoaderFactory.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Factories
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO.Abstractions;
    using Velaptor.Content;
    using Velaptor.NativeInterop.FreeType;
    using Velaptor.NativeInterop.OpenGL;
    using Velaptor.Services;
    using IVelaptorSound = Velaptor.Content.ISound;

    /// <summary>
    /// Creates instances of a content loader.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ContentLoaderFactory
    {
        private static IContentLoader? contentLoader;
        private static ILoader<ITexture>? textureLoader;
        private static ILoader<IAtlasData>? atlasLoader;
        private static ILoader<IVelaptorSound>? soundLoader;
        private static ILoader<IFont>? fontLoader;

        /// <summary>
        /// Creates a single instance of a content loader.
        /// </summary>
        /// <returns>A framework content loader implementation.</returns>
        public static IContentLoader CreateContentLoader() =>
            contentLoader ??= new ContentLoader(
                CreateTextureLoader(),
                CreateSoundLoader(),
                CreateTextureAtlasLoader(),
                CreateFontLoader());

        /// <summary>
        /// Creates a loader that loads textures from disk.
        /// </summary>
        /// <returns>A loader for loading textures.</returns>
        public static ILoader<ITexture> CreateTextureLoader()
        {
            if (textureLoader is not null)
            {
                return textureLoader;
            }

            var glInvoker = IoC.Container.GetInstance<IGLInvoker>();
            var imageService = IoC.Container.GetInstance<IImageService>();
            var texturePathResolver = new TexturePathResolver(IoC.Container.GetInstance<IDirectory>());

            textureLoader = new TextureLoader(
                glInvoker,
                imageService,
                texturePathResolver);

            return textureLoader;
        }

        /// <summary>
        /// Creates a loader for loading atlas data from disk.
        /// </summary>
        /// <returns>A loader for loading texture atlas data.</returns>
        public static ILoader<IAtlasData> CreateTextureAtlasLoader()
        {
            if (atlasLoader is not null)
            {
                return atlasLoader;
            }

            var glInvoker = IoC.Container.GetInstance<IGLInvoker>();
            var atlasDataPathResolver = new AtlasJSONDataPathResolver(IoC.Container.GetInstance<IDirectory>());

            atlasLoader = new AtlasLoader(
                glInvoker,
                IoC.Container.GetInstance<IImageService>(),
                atlasDataPathResolver,
                IoC.Container.GetInstance<IFile>());

            return atlasLoader;
        }

        /// <summary>
        /// Creates a loader that loads sounds from disk.
        /// </summary>
        /// <returns>A loader for loading sound data.</returns>
        public static ILoader<IVelaptorSound> CreateSoundLoader()
        {
            if (soundLoader is not null)
            {
                return soundLoader;
            }

            var soundPathResolver = new SoundPathResolver(IoC.Container.GetInstance<IDirectory>());
            var soundFactory = IoC.Container.GetInstance<ISoundFactory>();

            soundLoader = new SoundLoader(soundPathResolver, soundFactory);

            return soundLoader;
        }

        /// <summary>
        /// Creates a loader that loads fonts from disk for rendering test.
        /// </summary>
        /// <returns>A loader for loading sound data.</returns>
        public static ILoader<IFont> CreateFontLoader()
        {
            if (fontLoader is not null)
            {
                return fontLoader;
            }

            var glInvoker = IoC.Container.GetInstance<IGLInvoker>();
            var freeTypeInvoker = IoC.Container.GetInstance<IFreeTypeInvoker>();
            var fontPathResolver = new FontPathResolver(IoC.Container.GetInstance<IDirectory>());
            var fontAtlasService = IoC.Container.GetInstance<IFontAtlasService>();

            fontLoader = new FontLoader(
                glInvoker,
                freeTypeInvoker,
                fontAtlasService,
                fontPathResolver,
                IoC.Container.GetInstance<IFile>(),
                IoC.Container.GetInstance<IImageService>());

            return fontLoader;
        }
    }
}
