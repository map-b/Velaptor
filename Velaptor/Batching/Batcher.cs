// <copyright file="Batcher.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Batching;

using System;
using System.Drawing;
using Carbonate.Fluent;
using Carbonate.NonDirectional;
using Carbonate.OneWay;
using Graphics.Renderers.Exceptions;
using NativeInterop.OpenGL;
using OpenGL;
using ReactableData;

/// <inheritdoc/>
internal sealed class Batcher : IBatcher
{
    private const string RenderExceptionMsg = "The renderer is not initialized.";
    private const uint InitialBatchSize = 1000;
    private readonly IGLInvoker glInvoker;
    private readonly IPushReactable glInitReactable;
    private readonly CachedValue<Color> cachedClearColor;
    private bool isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="Batcher"/> class.
    /// </summary>
    /// <param name="glInvoker">Invokes OpenGL functions.</param>
    /// <param name="glInitReactable">Gets a notification that OpenGL is initialized.</param>
    /// <param name="batchSizeReactable">Sends notifications of the batch sizes to the different types of renderers.</param>
    public Batcher(
        IGLInvoker glInvoker,
        IPushReactable glInitReactable,
        IPushReactable<BatchSizeData> batchSizeReactable)
    {
        ArgumentNullException.ThrowIfNull(glInvoker);
        ArgumentNullException.ThrowIfNull(glInitReactable);
        ArgumentNullException.ThrowIfNull(batchSizeReactable);

        this.glInvoker = glInvoker;
        this.glInitReactable = glInitReactable;

        var glInitSubscription = ISubscriptionBuilder.Create()
            .WithId(PushNotifications.GLInitializedId)
            .WithName(this.GetExecutionMemberName(nameof(PushNotifications.GLInitializedId)))
            .BuildNonReceive(() =>
            {
                if (this.isInitialized)
                {
                    return;
                }

                glInvoker.Enable(GLEnableCap.Blend);
                glInvoker.BlendFunc(GLBlendingFactor.SrcAlpha, GLBlendingFactor.OneMinusSrcAlpha);

                foreach (var batchType in Enum.GetValues<BatchType>())
                {
                    batchSizeReactable.Push(
                        new BatchSizeData { BatchSize = InitialBatchSize, TypeOfBatch = batchType },
                        PushNotifications.BatchSizeChangedId);
                }

                batchSizeReactable.Unsubscribe(PushNotifications.BatchSizeChangedId);

                if (this.cachedClearColor is not null)
                {
                    this.cachedClearColor.IsCaching = false;
                }

                this.isInitialized = true;
            });

        glInitReactable.Subscribe(glInitSubscription);

        this.cachedClearColor = new CachedValue<Color>(
            Color.FromArgb(255, 16, 29, 36),
            () =>
            {
                var colorValues = new float[4];
                this.glInvoker.GetFloat(GLGetPName.ColorClearValue, colorValues);

                var red = colorValues[0].MapValue(0, 1, 0, 255);
                var green = colorValues[1].MapValue(0, 1, 0, 255);
                var blue = colorValues[2].MapValue(0, 1, 0, 255);
                var alpha = colorValues[3].MapValue(0, 1, 0, 255);

                return Color.FromArgb((byte)alpha, (byte)red, (byte)green, (byte)blue);
            },
            value =>
            {
                var red = value.R.MapValue(0f, 255f, 0f, 1f);
                var green = value.G.MapValue(0f, 255f, 0f, 1f);
                var blue = value.B.MapValue(0f, 255f, 0f, 1f);
                var alpha = value.A.MapValue(0f, 255f, 0f, 1f);

                this.glInvoker.ClearColor(red, green, blue, alpha);
            });
   }

    /// <inheritdoc/>
    public Color ClearColor
    {
        get => this.cachedClearColor.GetValue();
        set => this.cachedClearColor.SetValue(value);
    }

    /// <inheritdoc/>
    public bool HasBegun { get; private set;  }

    /// <inheritdoc/>
    public void Begin()
    {
        if (!this.isInitialized)
        {
            throw new RendererException(RenderExceptionMsg);
        }

        HasBegun = true;
        this.glInitReactable.Push(PushNotifications.BatchHasBegunId);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        if (!this.isInitialized)
        {
            throw new RendererException(RenderExceptionMsg);
        }

        this.glInvoker.Clear(GLClearBufferMask.ColorBufferBit);
    }

    /// <inheritdoc/>
    public void End()
    {
        if (!this.isInitialized)
        {
            throw new RendererException(RenderExceptionMsg);
        }

        HasBegun = false;
        this.glInitReactable.Push(PushNotifications.BatchHasEndedId);
    }
}
