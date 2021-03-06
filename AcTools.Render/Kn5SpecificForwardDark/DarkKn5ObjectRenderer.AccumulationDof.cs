﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AcTools.Render.Base.Cameras;
using AcTools.Render.Base.PostEffects;
using AcTools.Render.Base.TargetTextures;
using AcTools.Render.Base.Utils;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using JetBrains.Annotations;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace AcTools.Render.Kn5SpecificForwardDark {
    public partial class DarkKn5ObjectRenderer {
        private static Vector2[] Reorder(List<Vector2> poisson) {
            return poisson.OrderBy(x => x.LengthSquared()).ToArray();
            // return poisson.OrderBy(x => x.X + 1f + ((x.Y + 1f) * 200).Round(10)).ToArray();
        }

        private void InitializeAccumulationDof() {
            _lazyAccumulationDofPoissonDiskSamples = Lazier.Create(() => Reorder(UniformPoissonDiskSampler.SampleCircle(_accumulationDofIterations)));
            _lazyAccumulationDofPoissonSquareSamples = Lazier.Create(() => UniformPoissonDiskSampler.SampleSquare(_accumulationDofIterations, false).ToArray());
        }

        #region Poisson disk for accumulation DOF, for “round” blur
        private Lazier<Vector2[]> _lazyAccumulationDofPoissonDiskSamples;
        private int _accumulationDofPoissonPreviousDiskSample;

        [NotNull]
        private Vector2[] AccumulationDofPoissonDiskSamples => _lazyAccumulationDofPoissonDiskSamples.Value ?? new Vector2[2];

        private Vector2 NextAccumulationDofPoissonDiskSample() {
            var samples = AccumulationDofPoissonDiskSamples;
            _accumulationDofPoissonPreviousDiskSample = ++_accumulationDofPoissonPreviousDiskSample % samples.Length;
            return samples[_accumulationDofPoissonPreviousDiskSample];
        }
        #endregion

        #region Poisson square for accumulation AA, for square pixels
        private Lazier<Vector2[]> _lazyAccumulationDofPoissonSquareSamples;
        private int _accumulationDofPoissonPreviousSquareSample;

        [NotNull]
        private Vector2[] AccumulationDofPoissonSquareSamples => _lazyAccumulationDofPoissonSquareSamples.Value ?? new Vector2[2];

        private Vector2 NextAccumulationDofPoissonSquareSample() {
            var samples = AccumulationDofPoissonSquareSamples;
            _accumulationDofPoissonPreviousSquareSample = ++_accumulationDofPoissonPreviousSquareSample % samples.Length;
            return samples[_accumulationDofPoissonPreviousSquareSample];
        }
        #endregion

        private bool _useAccumulationDof;

        public bool UseAccumulationDof {
            get => _useAccumulationDof;
            set {
                if (Equals(value, _useAccumulationDof)) return;
                _useAccumulationDof = value;
                IsDirty = true;
                OnPropertyChanged();
            }
        }

        private int _accumulationDofIterations = 100;

        public int AccumulationDofIterations {
            get => _accumulationDofIterations;
            set {
                value = Math.Max(value, 2);
                if (Equals(value, _accumulationDofIterations)) return;
                _accumulationDofIterations = value;
                IsDirty = true;
                OnPropertyChanged();
                _lazyAccumulationDofPoissonDiskSamples.Reset();
                _lazyAccumulationDofPoissonSquareSamples.Reset();
                _realTimeAccumulationSize = 0;
            }
        }

        private float _accumulationDofApertureSize = 0.02f;

        public float AccumulationDofApertureSize {
            get => _accumulationDofApertureSize;
            set {
                if (Equals(value, _accumulationDofApertureSize)) return;
                _accumulationDofApertureSize = value;
                IsDirty = true;
                OnPropertyChanged();
                _realTimeAccumulationSize = 0;
            }
        }

        private bool _accumulationDofBokeh;

        public bool AccumulationDofBokeh {
            get => _accumulationDofBokeh;
            set {
                if (Equals(value, _accumulationDofBokeh)) return;
                _accumulationDofBokeh = value;
                OnPropertyChanged();
                _realTimeAccumulationSize = 0;
            }
        }

        protected override bool CanShotWithoutExtraTextures => base.CanShotWithoutExtraTextures && (!UseDof || !UseAccumulationDof);

        private CameraBase GetDofAccumulationCamera(CameraBase camera, float apertureMultipler, Vector2 diskOffset, Vector2 squareOffset) {
            var aaOffset = Matrix.Translation(squareOffset.X / Width, squareOffset.Y / Height, 0f);
            var newCamera = new AccumulationDofCamera(camera.FovY) {
                Right = camera.Right,
                Up = camera.Up,
                Position = camera.Position,
                Look = camera.Look,
                Tilt = camera.Tilt,
                ApertureOffset = AccumulationDofApertureSize * apertureMultipler * diskOffset,
                FocusPlane = DofFocusPlane,
                CutProj = camera.CutProj.HasValue ? aaOffset * camera.CutProj : aaOffset
            };

            newCamera.SetLens(AspectRatio);
            newCamera.UpdateViewMatrix();
            return newCamera;
        }

        private IDisposable ReplaceCamera(CameraBase newCamera) {
            var camera = Camera;
            Camera = newCamera;
            return new ActionAsDisposable(() => { Camera = camera; });
        }

        // private TargetResourceTexture _bufferFDofShotBokeh;

        private void DrawDofShotAccumulation() {
            if (!_accumulationDofBokehShotInProcess) {
                base.DrawOverride();
                return;
            }

            DrawSceneToBuffer();

            var bufferF = InnerBuffer;
            if (bufferF == null) return;

            var result = AaPass(bufferF.View, RenderTargetView);
            if (result != null) {
                DeviceContextHolder.GetHelper<CopyHelper>().Draw(DeviceContextHolder, result, RenderTargetView);
            }
        }

        private bool _accumulationDofShotInProcess;
        private bool _accumulationDofBokehShotInProcess;

        protected override void DrawShot(RenderTargetView target, IProgress<double> progress, CancellationToken cancellation) {
            if (UseDof && UseAccumulationDof && target != null) {
                var copy = DeviceContextHolder.GetHelper<CopyHelper>();

                _useDof = false;
                _accumulationDofShotInProcess = true;
                _accumulationDofBokehShotInProcess = AccumulationDofBokeh;

                try {
                    if (IsDirty) {
                        _realTimeAccumulationSize = 0;
                    }

                    using (var summary = TargetResourceTexture.Create(Format.R32G32B32A32_Float))
                    using (var temporary = TargetResourceTexture.Create(Format.R16G16B16A16_Float)) {
                        summary.Resize(DeviceContextHolder, ActualWidth, ActualHeight, null);
                        temporary.Resize(DeviceContextHolder, ActualWidth, ActualHeight, null);
                        DeviceContext.ClearRenderTargetView(summary.TargetView, default(Color4));
                        DeviceContext.ClearRenderTargetView(temporary.TargetView, default(Color4));

                        var samples = AccumulationDofPoissonDiskSamples;
                        for (var i = 0; i < samples.Length; i++) {
                            if (cancellation.IsCancellationRequested) return;

                            using (ReplaceCamera(GetDofAccumulationCamera(Camera, 1f, samples[i], NextAccumulationDofPoissonSquareSample()))) {
                                progress?.Report(0.05 + 0.9 * i / samples.Length);
                                base.DrawShot(temporary.TargetView, progress, cancellation);
                            }

                            DeviceContext.OutputMerger.BlendState = DeviceContextHolder.States.AddState;
                            copy.DrawSqr(DeviceContextHolder, temporary.View, summary.TargetView, _accumulationDofBokehShotInProcess ? 50f : 1f);
                            DeviceContext.OutputMerger.BlendState = null;
                        }

                        if (_accumulationDofBokehShotInProcess) {
                            copy.AccumulateDivide(DeviceContextHolder, summary.View, temporary.TargetView, samples.Length);
                            var bufferAColorGrading = PpColorGradingBuffer;
                            if (!UseColorGrading || bufferAColorGrading == null) {
                                var hdrView = HdrPass(temporary.View, target, OutputViewport);
                                if (hdrView != null) {
                                    copy.Draw(DeviceContextHolder, hdrView, target);
                                }
                            } else {
                                var hdrView = HdrPass(temporary.View, bufferAColorGrading.TargetView, bufferAColorGrading.Viewport) ?? bufferAColorGrading.View;
                                var colorGradingView = ColorGradingPass(hdrView, target, OutputViewport);
                                if (colorGradingView != hdrView) {
                                    copy.Draw(DeviceContextHolder, colorGradingView, target);
                                }
                            }
                        } else {
                            copy.AccumulateDivide(DeviceContextHolder, summary.View, target, samples.Length);
                        }
                    }

                } finally {
                    _useDof = true;
                    _accumulationDofShotInProcess = false;
                    _accumulationDofBokehShotInProcess = false;
                }

                return;
            }

            base.DrawShot(target, progress, cancellation);
        }

        public override bool AccumulationMode => UseDof && UseAccumulationDof && _realTimeAccumulationSize < AccumulationDofIterations;

        protected override void OnTickOverride(float dt) {
            base.OnTickOverride(dt);

            foreach (var light in _movingLights) {
                IsDirty |= light.Update();
            }

            if (IsDirty) {
                _realTimeAccumulationSize = 0;
            }
        }

        public override void Draw() {
            if (IsPaused) return;
            if (UseDof && UseAccumulationDof) {
                _realTimeAccumulationMode = true;
                if (IsDirty) {
                    _realTimeAccumulationSize = 0;
                }

                base.Draw();
            } else {
                if (_realTimeAccumulationMode) {
                    DisposeHelper.Dispose(ref _accumulationTexture);
                    DisposeHelper.Dispose(ref _accumulationMaxTexture);
                    DisposeHelper.Dispose(ref _accumulationTemporaryTexture);
                    DisposeHelper.Dispose(ref _accumulationBaseTexture);
                    _realTimeAccumulationSize = 0;
                    _realTimeAccumulationMode = false;
                }

                base.Draw();
            }
        }
    }
}