﻿// <copyright file="Window.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.UI
{
    // ReSharper disable RedundantNameQualifier
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Numerics;
    using System.Threading.Tasks;
    using Velaptor.Content;
    using Velaptor.Observables;
    using VelObservable = Velaptor.Observables.Core.IObservable<bool>;

    // ReSharper restore RedundantNameQualifier

    /// <summary>
    /// A system window where graphics can be rendered.
    /// </summary>
    public abstract class Window : IWindowProps, IDisposable
    {
        private readonly IWindow window;
        private readonly VelObservable shutDownObservable;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        /// <param name="window">The window implementation that contains the window functionality.</param>
        /// <param name="shutDownObservable">Sends out a notification that the application is shutting down.</param>
        internal Window(IWindow window, VelObservable shutDownObservable)
        {
            this.window = window ?? throw new ArgumentNullException(nameof(window), "Window must not be null.");
            this.shutDownObservable = shutDownObservable;

            this.window.Initialize = OnLoad;
            this.window.Uninitialize = OnUnload;
            this.window.Update = OnUpdate;
            this.window.Draw = OnDraw;
            this.window.WinResize = OnResize;

            // Set the update frequency to default value of 60
            // just in case the IWindow implementation is not
            this.window.UpdateFrequency = 60;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        /// <param name="window">The window implementation that contains the window functionality.</param>
        [ExcludeFromCodeCoverage]
        protected Window(IWindow window)
        {
            this.window = window ?? throw new ArgumentNullException(nameof(window), "Window must not be null.");
            this.shutDownObservable = IoC.Container.GetInstance<ShutDownObservable>();

            this.window.Initialize = OnLoad;
            this.window.Uninitialize = OnUnload;
            this.window.Update = OnUpdate;
            this.window.Draw = OnDraw;
            this.window.WinResize = OnResize;

            // Set the update frequency to default value of 60
            // just in case the IWindow implementation is not
            this.window.UpdateFrequency = 60;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Window"/> class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        ~Window()
        {
            if (UnitTestDetector.IsRunningFromUnitTest)
            {
                return;
            }

            Dispose(false);
        }

        /// <inheritdoc/>
        public string Title
        {
            get => this.window.Title;
            set => this.window.Title = value;
        }

        /// <inheritdoc/>
        public Vector2 Position
        {
            get => this.window.Position;
            set => this.window.Position = value;
        }

        /// <inheritdoc/>
        public uint Width
        {
            get => this.window.Width;
            set => this.window.Width = value;
        }

        /// <inheritdoc/>
        public uint Height
        {
            get => this.window.Height;
            set => this.window.Height = value;
        }

        /// <inheritdoc/>
        public int UpdateFrequency
        {
            get => this.window.UpdateFrequency;
            set => this.window.UpdateFrequency = value;
        }

        /// <inheritdoc/>
        public bool AutoClearBuffer
        {
            get => this.window.AutoClearBuffer;
            set => this.window.AutoClearBuffer = value;
        }

        /// <inheritdoc/>
        public bool MouseCursorVisible
        {
            get => this.window.MouseCursorVisible;
            set => this.window.MouseCursorVisible = value;
        }

        /// <inheritdoc/>
        public StateOfWindow WindowState
        {
            get => this.window.WindowState;
            set => this.window.WindowState = value;
        }

        /// <inheritdoc/>
        public WindowBorder TypeOfBorder
        {
            get => this.window.TypeOfBorder;
            set => this.window.TypeOfBorder = value;
        }

        /// <inheritdoc/>
        public IContentLoader ContentLoader
        {
            get => this.window.ContentLoader;
            set => this.window.ContentLoader = value;
        }

        /// <inheritdoc/>
        public bool Initialized => this.window.Initialized;

        /// <summary>
        /// Shows the window.
        /// </summary>
        public void Show() => this.window.Show();

        /// <summary>
        /// Shows the window asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        /// <remarks>
        ///     This runs the window on another thread.
        /// </remarks>
        public async Task ShowAsync() => await this.window.ShowAsync().ConfigureAwait(true);

        /// <summary>
        /// <inheritdoc cref="IDisposable.Dispose"/>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Invoked when the window is loaded.
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected virtual void OnLoad()
        {
        }

        /// <summary>
        /// Invoked when the window is updated.
        /// </summary>
        /// <param name="frameTime">The amount of time that has passed for the current frame.</param>
        [ExcludeFromCodeCoverage]
        protected virtual void OnUpdate(FrameTime frameTime)
        {
        }

        /// <summary>
        /// Invoked when the window renders its content.
        /// </summary>
        /// <param name="frameTime">The amount of time that has passed for the current frame.</param>
        [ExcludeFromCodeCoverage]
        protected virtual void OnDraw(FrameTime frameTime)
        {
        }

        /// <summary>
        /// Invoked when the window is unloaded.
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected virtual void OnUnload() => this.shutDownObservable.PushNotification(true);

        /// <summary>
        /// Invoked when the window size changes.
        /// </summary>
        /// <param name="size">The new size.</param>
        [ExcludeFromCodeCoverage]
        protected virtual void OnResize(SizeU size)
        {
        }

        /// <summary>
        /// <inheritdoc cref="IDisposable.Dispose"/>
        /// </summary>
        /// <param name="disposing">Disposes managed resources when <see langword="true"/>.</param>
        private void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (disposing)
            {
                this.window.Dispose();
            }

            this.isDisposed = true;
        }
    }
}
