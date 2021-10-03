// <copyright file="SceneManager.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace VelaptorTesting.Core
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Velaptor;
    using Velaptor.Content;
    using Velaptor.Graphics;
    using Velaptor.UI;

    // TODO: Setup this class to be IDisposable
    public sealed class SceneManager : IUpdatable, IDisposable
    {
        private readonly List<IScene> scenes = new ();
        private readonly Button nextButton;
        private readonly Button previousButton;
        private ISpriteBatch spriteBatch;
        private int currentSceneIndex;
        private bool isDisposed;
        private bool isLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneManager"/> class.
        /// </summary>
        /// <param name="contentLoader">The loads all of the content for the scenes.</param>
        /// <param name="spriteBatch">The renders all of the scenes.</param>
        public SceneManager(IContentLoader contentLoader, ISpriteBatch spriteBatch)
        {
            this.spriteBatch = spriteBatch;

            // TODO: Improve this by creating a control factory
            var nextButtonLabel = new Label(contentLoader) { Text = "-->" };
            this.nextButton = new Button(contentLoader, nextButtonLabel);
            this.nextButton.Click += (_, e) => NextScene();

            var previousButtonLabel = new Label(contentLoader) { Text = "<--" };
            this.previousButton = new Button(contentLoader, previousButtonLabel);
            this.previousButton.Click += (_, e) => PreviousScene();
        }

        /// <summary>
        /// Gets the current scene.
        /// </summary>
        public IScene? CurrentScene => this.scenes.Count <= 0 ? null : this.scenes[this.currentSceneIndex];

        /// <summary>
        /// Adds the given scene.
        /// </summary>
        /// <param name="scene">The scene to add.</param>
        /// <exception cref="Exception">
        ///     Thrown if a scene with with the given <paramref name="scene"/>'s ID already exists.
        /// </exception>
        public void AddScene(IScene scene)
        {
            if (SceneExists(scene.ID))
            {
                throw new Exception($"The sceneBase '{scene.Name}' already exists.");
            }

            this.scenes.Add(scene);
        }

        /// <summary>
        /// Removes a scene that matches the given scene ID.
        /// </summary>
        /// <param name="sceneId">The ID of the scene to remove.</param>
        public void RemoveScene(Guid sceneId)
        {
            if (SceneExists(sceneId) is false)
            {
                return;
            }

            this.scenes.Remove(this.scenes.FirstOrDefault(s => s.ID == sceneId));
        }

        /// <summary>
        /// Removes a scene that matches the given scene ID.
        /// </summary>
        /// <param name="scene">The scene to remove.</param>
        public void RemoveScene(IScene scene) => RemoveScene(scene.ID);

        private int nextHitCount = 0;

        /// <summary>
        /// Moves to the next scene.
        /// </summary>
        public void NextScene()
        {
            this.nextHitCount += 1;
            if (this.scenes.Count <= 0)
            {
                return;
            }

            var previousScene = this.currentSceneIndex;
            this.currentSceneIndex = this.currentSceneIndex >= this.scenes.Count - 1
                ? 0 : this.currentSceneIndex + 1;

            this.scenes[previousScene].IsActive = false;
            this.scenes[previousScene].UnloadContent();

            this.scenes[this.currentSceneIndex].IsActive = true;
            this.scenes[this.currentSceneIndex].LoadContent();
        }

        /// <summary>
        /// Moves to the previous scene.
        /// </summary>
        public void PreviousScene()
        {
            if (this.scenes.Count <= 0)
            {
                return;
            }

            var previousScene = this.currentSceneIndex;
            this.currentSceneIndex = this.currentSceneIndex <= 0
                ? this.scenes.Count - 1 : this.currentSceneIndex - 1;

            this.scenes[previousScene].IsActive = false;
            this.scenes[previousScene].UnloadContent();

            this.scenes[this.currentSceneIndex].IsActive = true;
            this.scenes[this.currentSceneIndex].LoadContent();
        }

        /// <summary>
        /// Loads the content for the manager and the current scene.
        /// </summary>
        public void LoadContent()
        {
            if (this.isDisposed)
            {
                throw new Exception("Cannot load a scene manager that has been disposed.");
            }

            if (this.isLoaded)
            {
                return;
            }

            this.scenes[this.currentSceneIndex].LoadContent();
            this.nextButton.LoadContent();
            this.previousButton.LoadContent();

            const int buttonSpacing = 15;
            const int rightMargin = 15;

            var buttonTops = MainWindow.WindowHeight - (new[] { this.nextButton.Height, this.previousButton.Height }.Max() + 20);
            var buttonGroupLeft = MainWindow.WindowWidth - (this.nextButton.Width + this.previousButton.Width + buttonSpacing + rightMargin);
            this.previousButton.Position = new Point(buttonGroupLeft, buttonTops);
            this.nextButton.Position = new Point(this.previousButton.Position.X + this.previousButton.Width + buttonSpacing, buttonTops);

            this.isLoaded = true;
        }

        /// <summary>
        /// Unloads the scene manager content and scenes added.
        /// </summary>
        public void UnloadContent()
        {
            if (!this.isLoaded || this.isDisposed)
            {
                return;
            }

            DisposeOrUnloadContent();
        }

        /// <summary>
        /// Updates the active scenes.
        /// </summary>
        /// <param name="frameTime">The amount of time passed for the current frame.</param>
        public void Update(FrameTime frameTime)
        {
            if (this.scenes.Count <= 0)
            {
                return;
            }

            this.nextButton.Update(frameTime);
            this.previousButton.Update(frameTime);

            this.scenes[this.currentSceneIndex].Update(frameTime);
        }

        /// <summary>
        /// Renders the active scenes.
        /// </summary>
        public void Render()
        {
            if (this.scenes.Count <= 0)
            {
                return;
            }

            this.spriteBatch.Clear();
            this.spriteBatch.BeginBatch();

            this.scenes[this.currentSceneIndex].Render(this.spriteBatch);

            // Render the scene manager UI on top of all other textures
            this.nextButton.Render(this.spriteBatch);
            this.previousButton.Render(this.spriteBatch);

            this.spriteBatch.EndBatch();
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            DisposeOrUnloadContent();

            this.isDisposed = true;
        }

        /// <summary>
        /// Returns a value indicating whether a scene with the given ID already exists.
        /// </summary>
        /// <param name="id">The ID of the scene to check for.</param>
        /// <returns>True if the scene exists.</returns>
        private bool SceneExists(Guid id) => this.scenes.Any(s => s.ID == id);

        /// <summary>
        /// Disposes or unloads all of the scene content.
        /// </summary>
        private void DisposeOrUnloadContent()
        {
            foreach (var scene in this.scenes)
            {
                scene.UnloadContent();
            }

            this.scenes.Clear();

            this.spriteBatch = null;
            this.previousButton.Dispose();
            this.nextButton.Dispose();
        }
    }
}
