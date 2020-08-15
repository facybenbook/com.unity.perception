﻿using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("Vector4")]
    public class Vector4Parameter : NumericParameter<Vector4>
    {
        [SerializeReference] public ISampler x = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler y = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler z = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler w = new UniformSampler(0f, 1f);

        public override ISampler[] samplers => new []{ x, y, z, w };

        public override Vector4 Sample()
        {
            return new Vector4(x.Sample(), y.Sample(), z.Sample(), w.Sample());
        }

        public override NativeArray<Vector4> Samples(int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector4>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.Samples(totalSamples, out var xHandle);
            var yRng = y.Samples(totalSamples, out var yHandle);
            var zRng = z.Samples(totalSamples, out var zHandle);
            var wRng = w.Samples(totalSamples, out var wHandle);

            var handles = new NativeArray<JobHandle>(4, Allocator.Temp)
            {
                [0] = xHandle,
                [1] = yHandle,
                [2] = zHandle,
                [3] = wHandle
            };
            var combinedJobHandles = JobHandle.CombineDependencies(handles);
            handles.Dispose();

            jobHandle = new SamplesJob
            {
                xRng = xRng,
                yRng = yRng,
                zRng = zRng,
                wRng = wRng,
                samples = samples
            }.Schedule(combinedJobHandles);
            return samples;
        }

        [BurstCompile]
        struct SamplesJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> xRng;
            [DeallocateOnJobCompletion] public NativeArray<float> yRng;
            [DeallocateOnJobCompletion] public NativeArray<float> zRng;
            [DeallocateOnJobCompletion] public NativeArray<float> wRng;
            public NativeArray<Vector4> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = new Vector4(xRng[i], yRng[i], zRng[i], wRng[i]);
            }
        }
    }
}