using System;
using UnityEngine;

namespace IndustrialDemo.Core
{
    public readonly struct NoiseEvent
    {
        public NoiseEvent(Vector3 position, float intensity, GameObject source, string category)
        {
            Position = position;
            Intensity = intensity;
            Source = source;
            Category = category ?? string.Empty;
        }

        public Vector3 Position { get; }
        public float Intensity { get; }
        public GameObject Source { get; }
        public string Category { get; }
    }

    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> NoiseEmitted;

        public static void Emit(Vector3 position, float intensity, GameObject source = null, string category = "")
        {
            NoiseEmitted?.Invoke(new NoiseEvent(position, Mathf.Max(0f, intensity), source, category));
        }
    }
}
