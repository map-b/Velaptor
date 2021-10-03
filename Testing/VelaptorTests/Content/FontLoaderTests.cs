﻿// <copyright file="FontLoaderTests.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace VelaptorTests.Content
{
    using System.Drawing;
    using System.IO;
    using System.IO.Abstractions;
    using Moq;
    using Newtonsoft.Json;
    using Velaptor.Content;
    using Velaptor.Content.Exceptions;
    using Velaptor.Graphics;
    using Velaptor.NativeInterop.FreeType;
    using Velaptor.NativeInterop.OpenGL;
    using Velaptor.Services;
    using VelaptorTests.Helpers;
    using Xunit;

    /// <summary>
    /// Tests the <see cref="FontLoader"/> class.
    /// </summary>
    public class FontLoaderTests
    {
        private const string FontContentName = "test-font";
        private readonly string fontsDirPath;
        private readonly string fontDataFilePath;
        private readonly GlyphMetrics[] glyphMetricData;
        private readonly Mock<IGLInvoker> mockGLInvoker;
        private readonly Mock<IFreeTypeInvoker> mockFreeTypeInvoker;
        private readonly Mock<IFontAtlasService> mockFontAtlasService;
        private readonly Mock<IFile> mockFile;
        private readonly Mock<IPathResolver> mockFontPathResolver;
        private readonly Mock<IImageService> mockImageService;
        private readonly JsonSerializerSettings jsonSettings = new ()
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Objects,
        };
        private readonly FontSettings fontSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontLoaderTests"/> class.
        /// </summary>
        public FontLoaderTests()
        {
            this.fontsDirPath = @"C:\temp\Content\Fonts\";
            this.fontDataFilePath = $@"{this.fontsDirPath}{FontContentName}.json";
            var fontFilepath = $@"{this.fontsDirPath}{FontContentName}.ttf";

            this.mockGLInvoker = new Mock<IGLInvoker>();
            this.mockFreeTypeInvoker = new Mock<IFreeTypeInvoker>();

            this.glyphMetricData = new GlyphMetrics[]
                {
                    GenerateMetricData(0),
                    GenerateMetricData(10),
                };

            this.mockFontAtlasService = new Mock<IFontAtlasService>();
            this.mockFontAtlasService.Setup(m => m.CreateFontAtlas(fontFilepath, It.IsAny<int>()))
                .Returns(() => (default(ImageData), this.glyphMetricData));

            this.mockFontPathResolver = new Mock<IPathResolver>();
            this.mockFontPathResolver.Setup(m => m.ResolveDirPath()).Returns(this.fontsDirPath);

            this.fontSettings = new FontSettings
            {
                Size = 12,
                Style = FontStyle.Regular,
            };

            this.mockFile = new Mock<IFile>();
            this.mockFile.Setup(m => m.Exists(this.fontDataFilePath)).Returns(true);
            this.mockFile.Setup(m => m.ReadAllText(this.fontDataFilePath)).Returns(()
                => JsonConvert.SerializeObject(this.fontSettings, this.jsonSettings));

            this.mockImageService = new Mock<IImageService>();
        }

        #region Method Tests
        [Fact]
        public void Load_WhenInvoked_LoadsFont()
        {
            // Arrange
            var loader = CreateLoader();

            // Act
            var actual = loader.Load(FontContentName);

            // Assert
            this.mockFile.Verify(m => m.ReadAllText(this.fontDataFilePath), Times.Once());
            this.mockImageService.Verify(m => m.FlipVertically(It.IsAny<ImageData>()), Times.Once());
            Assert.Equal(this.fontSettings.Size, actual.Size);
            Assert.Equal(this.fontSettings.Style, actual.Style);
            Assert.Equal(FontContentName, actual.Name);
            Assert.Equal($"{this.fontsDirPath}{FontContentName}", actual.Path);
            Assert.Equal(this.glyphMetricData.Length, actual.Metrics.Count);
        }

        [Fact]
        public void Load_WhenInvoked_SetsFontAsPooled()
        {
            // Arrange
            var loader = CreateLoader();

            // Act
            var actual = loader.Load(FontContentName);

            // Assert
            Assert.True(actual.IsPooled);
            Assert.True(actual.FontTextureAtlas.IsPooled);
        }

        [Fact]
        public void Load_WithAlreadyLoadedAtlasData_ReturnsAlreadyLoadedAtlasData()
        {
            // Arrange
            var loader = CreateLoader();
            loader.Load(FontContentName);

            // Act
            var actual = loader.Load(FontContentName);

            // Assert
            this.mockFile.Verify(m => m.ReadAllText(this.fontDataFilePath), Times.Once());
            Assert.Equal(FontContentName, actual.Name);
        }

        [Fact]
        public void Load_WhenLoadingSameDataThatIsDisposed_RemovesDataBeforeAdding()
        {
            // Arrange
            var loader = CreateLoader();
            var loadedFont = loader.Load(FontContentName);

            // Set the font as not being pooled. This will allow us to dispose of
            // the font to get the font into the disposed state for testing
            loadedFont.IsPooled = false;
            loadedFont.Dispose();

            // Act
            var actual = loader.Load(FontContentName);

            // Assert
            Assert.NotSame(loadedFont, actual);
        }

        [Fact]
        public void Load_WithMissingFontJSONDataFile_ThrowsException()
        {
            // Arrange
            this.mockFile.Setup(m => m.Exists(this.fontDataFilePath)).Returns(false);

            var loader = CreateLoader();

            // Act & Assert
            AssertExtensions.ThrowsWithMessage<FileNotFoundException>(() =>
            {
                loader.Load(FontContentName);
            }, $"The JSON data file '{this.fontDataFilePath}' describing the font settings for font content '{FontContentName}' is missing.");
        }

        [Fact]
        public void Load_WithIssuesDeserializingJSONAtlasData_ThrowsException()
        {
            // Arrange
            this.mockFile.Setup(m => m.ReadAllText(this.fontDataFilePath))
                .Returns(() => "invalid-data");

            var loader = CreateLoader();
            const string newtonsoftErrorMsg = "Unexpected character encountered while parsing value: i. Path '', line 0, position 0.";

            // Act & Assert
            AssertExtensions.ThrowsWithMessage<LoadContentException>(() =>
            {
                loader.Load(FontContentName);
            }, $"There was an issue deserializing the JSON atlas data file at '{this.fontDataFilePath}'.\n{newtonsoftErrorMsg}");
        }

        [Fact]
        public void Load_WithNullDeserializeResult_ThrowsException()
        {
            // Arrange
            this.mockFile.Setup(m => m.ReadAllText(this.fontDataFilePath))
                .Returns(() => string.Empty);

            var loader = CreateLoader();
            const string exceptionMessage = "Deserialized font settings are null.";

            // Act & Assert
            AssertExtensions.ThrowsWithMessage<LoadContentException>(() =>
            {
                loader.Load(FontContentName);
            }, $"There was an issue deserializing the JSON atlas data file at '{this.fontDataFilePath}'.\n{exceptionMessage}");
        }

        [Fact]
        public void Unload_WhenInvoked_UnloadsFonts()
        {
            // Arrange
            var loader = CreateLoader();
            var font = loader.Load(FontContentName);

            // Act
            loader.Unload(FontContentName);

            // Assert
            Assert.True(font.IsDisposed);
        }

        [Fact]
        public void Dispose_WhenInvoked_DisposesOfTextures()
        {
            // Arrange
            var loader = CreateLoader();
            var font = loader.Load(FontContentName);

            // Act
            loader.Dispose();
            loader.Dispose();

            // Assert
            Assert.True(font.IsDisposed);
        }
        #endregion

        /// <summary>
        /// Generates fake glyph metric data for testing.
        /// </summary>
        /// <param name="start">The start value of all of the metric data.</param>
        /// <returns>The glyph metric data to be tested.</returns>
        /// <remarks>
        ///     The start value is a metric value start and incremented for each metric.
        /// </remarks>
        private static GlyphMetrics GenerateMetricData(int start)
        {
            return new GlyphMetrics()
            {
                Ascender = start,
                Descender = start + 1,
                CharIndex = (uint)start + 2,
                GlyphWidth = start + 3,
                GlyphHeight = start + 4,
                HoriBearingX = start + 5,
                HoriBearingY = start + 6,
                XMin = start + 7,
                XMax = start + 8,
                YMin = start + 9,
                YMax = start + 10,
                HorizontalAdvance = start + 11,
                Glyph = (char)(start + 12),
                AtlasBounds = new Rectangle(start + 13, start + 14, start + 15, start + 16),
            };
        }

        /// <summary>
        /// Creates an instance of <see cref="AtlasLoader"/> for the purpoase of testing.
        /// </summary>
        /// <returns>The instnace to test.</returns>
        private FontLoader CreateLoader() => new (
            this.mockGLInvoker.Object,
            this.mockFreeTypeInvoker.Object,
            this.mockFontAtlasService.Object,
            this.mockFontPathResolver.Object,
            this.mockFile.Object,
            this.mockImageService.Object);
    }
}
