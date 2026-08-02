using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;

namespace TF.State.Domain.Context
{
    public static class RenderInterpolation
    {
        private const float SnapDistance = 48f;

        private static Dictionary<Entity, Vector2> _previous = new Dictionary<Entity, Vector2>();
        private static Dictionary<Entity, Vector2> _current = new Dictionary<Entity, Vector2>();

        private static readonly List<Entity> _shifted = new List<Entity>();
        private static readonly List<Vector2> _shiftedFrom = new List<Vector2>();

        private static readonly FieldInfo _particlesField = typeof(ParticleSystem).GetField("particles", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly List<Particle[]> _particleArrays = new List<Particle[]>();
        private static readonly List<int> _particleIndices = new List<int>();
        private static readonly List<Vector2> _particleFrom = new List<Vector2>();

        private const float MaxSpin = 1f;

        private static Dictionary<GraphicsComponent, float> _previousSpin = new Dictionary<GraphicsComponent, float>();
        private static Dictionary<GraphicsComponent, float> _currentSpin = new Dictionary<GraphicsComponent, float>();

        private static readonly List<GraphicsComponent> _spun = new List<GraphicsComponent>();
        private static readonly List<float> _spunFrom = new List<float>();

        private static Vector2 _previousCamera;
        private static Vector2 _currentCamera;
        private static Vector2 _cameraBefore;
        private static bool _cameraShifted;

        private static Scene _appliedScene;
        private static bool _hasBaseline;
        private static bool _stepped;

        public static void Invalidate()
        {
            Restore();

            _previous.Clear();
            _current.Clear();
            _previousSpin.Clear();
            _currentSpin.Clear();
            _hasBaseline = false;
            _stepped = false;
        }

        public static void BeginStep()
        {
            _stepped = false;
        }

        public static void Capture(Scene scene)
        {
            if (scene == null)
            {
                return;
            }

            var recycled = _previous;
            _previous = _current;
            _current = recycled;
            _current.Clear();

            var recycledSpin = _previousSpin;
            _previousSpin = _currentSpin;
            _currentSpin = recycledSpin;
            _currentSpin.Clear();

            foreach (var layer in scene.Layers.Values)
            {
                var entities = layer.Entities;

                for (int i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];

                    _current[entity] = entity.Position;

                    var components = entity.Components;

                    if (components == null)
                    {
                        continue;
                    }

                    for (int c = 0; c < components.Count; c++)
                    {
                        if (components[c] is GraphicsComponent graphics && graphics.Rotation != 0f)
                        {
                            _currentSpin[graphics] = graphics.Rotation;
                        }
                    }
                }
            }

            _previousCamera = _currentCamera;
            _currentCamera = scene.Camera.Position;

            _hasBaseline = _previous.Count > 0;
            _stepped = true;
        }

        public static void Apply(Scene scene, float alpha)
        {
            Restore();

            if (scene == null || !_hasBaseline || !_stepped || alpha >= 1f || alpha <= 0f)
            {
                return;
            }

            foreach (var layer in scene.Layers.Values)
            {
                var entities = layer.Entities;

                for (int i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];

                    if (entity is ParticleSystem system)
                    {
                        ApplyParticles(system, alpha);
                    }

                    ApplySpin(entity, alpha);

                    if (!_previous.TryGetValue(entity, out var previous) || !_current.TryGetValue(entity, out var current))
                    {
                        continue;
                    }

                    if (current != entity.Position)
                    {
                        continue;
                    }

                    var delta = current - previous;

                    if (delta == Vector2.Zero || Math.Abs(delta.X) > SnapDistance || Math.Abs(delta.Y) > SnapDistance)
                    {
                        continue;
                    }

                    _shifted.Add(entity);
                    _shiftedFrom.Add(current);

                    entity.Position = previous + delta * alpha;
                }
            }

            var cameraDelta = _currentCamera - _previousCamera;

            if (cameraDelta != Vector2.Zero && Math.Abs(cameraDelta.X) <= SnapDistance && Math.Abs(cameraDelta.Y) <= SnapDistance)
            {
                _cameraBefore = scene.Camera.Position;
                _cameraShifted = true;

                scene.Camera.Position = _previousCamera + cameraDelta * alpha;
            }

            _appliedScene = scene;
        }

        private static void ApplySpin(Entity entity, float alpha)
        {
            var components = entity.Components;

            if (components == null)
            {
                return;
            }

            for (int c = 0; c < components.Count; c++)
            {
                if (components[c] is not GraphicsComponent graphics)
                {
                    continue;
                }

                if (!_previousSpin.TryGetValue(graphics, out var previous)
                    || !_currentSpin.TryGetValue(graphics, out var current))
                {
                    continue;
                }

                if (current != graphics.Rotation)
                {
                    continue;
                }

                var delta = current - previous;

                if (delta == 0f || Math.Abs(delta) > MaxSpin)
                {
                    continue;
                }

                _spun.Add(graphics);
                _spunFrom.Add(current);

                graphics.Rotation = previous + delta * alpha;
            }
        }

        private static void ApplyParticles(ParticleSystem system, float alpha)
        {
            if (_particlesField?.GetValue(system) is not Particle[] particles)
            {
                return;
            }

            var back = 1f - alpha;

            for (int i = 0; i < particles.Length; i++)
            {
                if (!particles[i].Active)
                {
                    continue;
                }

                var speed = particles[i].Speed;

                if (speed == Vector2.Zero)
                {
                    continue;
                }

                _particleArrays.Add(particles);
                _particleIndices.Add(i);
                _particleFrom.Add(particles[i].Position);

                particles[i].Position -= speed * back;
            }
        }

        public static void Restore()
        {
            for (int i = 0; i < _shifted.Count; i++)
            {
                _shifted[i].Position = _shiftedFrom[i];
            }

            for (int i = 0; i < _particleArrays.Count; i++)
            {
                _particleArrays[i][_particleIndices[i]].Position = _particleFrom[i];
            }

            for (int i = 0; i < _spun.Count; i++)
            {
                _spun[i].Rotation = _spunFrom[i];
            }

            _spun.Clear();
            _spunFrom.Clear();

            _particleArrays.Clear();
            _particleIndices.Clear();
            _particleFrom.Clear();

            _shifted.Clear();
            _shiftedFrom.Clear();

            if (_cameraShifted && _appliedScene != null)
            {
                _appliedScene.Camera.Position = _cameraBefore;
            }

            _cameraShifted = false;
            _appliedScene = null;
        }
    }
}
