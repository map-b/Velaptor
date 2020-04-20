﻿using Raptor.Graphics;
using Raptor.Plugins;
using System;
using System.Numerics;

namespace Raptor.SDLImp
{
    /// <summary>
    /// Provides the ability to draw frames around a physics body shape for debugging purposes.
    /// </summary>
    public class SDLDebugDraw : IDebugDraw
    {
        #region Public Methods
        /// <summary>
        /// Draws a white outline around the given <paramref name="body"/> using the given <paramref name="renderer"/>.
        /// </summary>
        /// <param name="renderer">The renderer to use for rendering the outline/frame.</param>
        /// <param name="body">The body to render the outline/frame around.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public void Draw(IRenderer renderer, IPhysicsBody body)
        {
            if (renderer is null)
                throw new ArgumentNullException(nameof(renderer), "The renderer must not be null.");

            if (body is null)
                throw new ArgumentNullException(nameof(body), "The physics body must not be null.");

            int max = body.XVertices.Count;

            var origin = new Vector2(body.X, body.Y);

            for (int i = 0; i < max; i++)
            {
                var start = new Vector2(body.XVertices[i], body.YVertices[i]).RotateAround(origin, body.Angle);
                var stop = new Vector2(body.XVertices[i < max - 1 ? i + 1 : 0], body.YVertices[i < max - 1 ? i + 1 : 0]).RotateAround(origin, body.Angle);

                renderer.RenderLine(start.X, start.Y, stop.X, stop.Y);
            }
        }



        /// <summary>
        /// Draws an outline using the given <paramref name="color"/> around the given <paramref name="body"/> using the given <paramref name="renderer"/>.
        /// </summary>
        /// <param name="renderer">The renderer to use for rendering the outline/frame.</param>
        /// <param name="body">The body to render the outline/frame around.</param>
        /// <param name="color">The color of the outline</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public void Draw(IRenderer renderer, IPhysicsBody body, GameColor color)
        {
            if (renderer is null)
                throw new ArgumentNullException(nameof(renderer), "The renderer must not be null.");

            if (body is null)
                throw new ArgumentNullException(nameof(body), "The physics body must not be null.");

            int max = body.XVertices.Count;

            var origin = new Vector2(body.X, body.Y);

            for (int i = 0; i < max; i++)
            {
                var start = new Vector2(body.XVertices[i], body.YVertices[i]).RotateAround(origin, body.Angle);
                var stop = new Vector2(body.XVertices[i < max - 1 ? i + 1 : 0], body.YVertices[i < max - 1 ? i + 1 : 0]).RotateAround(origin, body.Angle);

                renderer.RenderLine(start.X, start.Y, stop.X, stop.Y, color);
            }
        }



        /// <summary>
        /// Gets the data as the given type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="option">Used to pass in options for the <see cref="GetData{T}(int)"/> implementation to process.</param>
        /// <typeparam name="T">The type of data to get.</typeparam>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        public static T GetData<T>(int option) where T : class => throw new NotImplementedException(option.ToString());



        /// <summary>
        /// Injects any arbitrary data into the plugin for use.  Must be a class.
        /// </summary>
        /// <typeparam name="T">The type of data to inject.</typeparam>
        /// <param name="data">The data to inject.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public void InjectData<T>(T data) where T : class => throw new NotImplementedException();
        #endregion
    }
}
