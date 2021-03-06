﻿using System;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUutilities.DataStructures;
using Engine;
using Engine.Audio;
using Engine.Behaviors;
using Engine.Physics;
using Veldrid.Assets;
using System.Numerics;

namespace CrazyCore
{
    public class BallAudio : Behavior
    {
        private AudioSourceComponent _rollSource;
        private AudioSourceComponent _thudSource;
        private Collider _collider;

        public float MaxVolume { get; set; } = 1.0f;
        public float MaxVelocityForVolume { get; set; } = 10.0f;
        public float ThudMaxImpulseForVolume { get; set; } = 50.0f;
        public AssetRef<WaveFile> ThudClip { get; set; }
        public float ThudThreshold { get; set; }

        protected override void Start(SystemRegistry registry)
        {
            _collider = GameObject.GetComponent<Collider>();
            _rollSource = GameObject.GetComponent<AudioSourceComponent>();
            _thudSource = new AudioSourceComponent();
            _thudSource.AudioClip = ThudClip;
            GameObject.AddComponent(_thudSource);
        }

        private void PlayThud(float volumeRatio)
        {
            _thudSource.Gain = volumeRatio * MaxVolume;
            _thudSource.Play();
        }

        public override void Update(float deltaSeconds)
        {
            ReadOnlyList<CollidablePairHandler> currentPairs = _collider.Entity.CollisionInformation.Pairs;
            Vector3 currentImpulse = CheckContactPairs(currentPairs);
            if (currentImpulse.LengthSquared() > 0f)
            {
                Vector3 linearVelocity = _collider.Entity.LinearVelocity;
                Vector3 tangentMotion = linearVelocity - MathUtil.Projection(linearVelocity, Vector3.Normalize(currentImpulse));
                float ratio = tangentMotion.Length() / MaxVelocityForVolume;
                if (_rollSource.Gain == 0.0f)
                {
                    _rollSource.Play();
                }

                float gain = ratio * MaxVolume;
                if (float.IsNaN(gain))
                {
                    gain = 1f;
                }

                _rollSource.Gain = gain;
            }
            else
            {
                _rollSource.Gain = 0f;
                _rollSource.Stop();
            }
        }

        private Vector3 CheckContactPairs(ReadOnlyList<CollidablePairHandler> pairs)
        {
            bool thudded = false;
            Vector3 totalImpulse = Vector3.Zero;
            foreach (var pair in pairs)
            {
                foreach (var contactInfo in pair.Contacts)
                {
                    totalImpulse += contactInfo.NormalImpulse * contactInfo.Contact.Normal;
                    if (contactInfo.NormalImpulse > ThudThreshold)
                    {
                        if (!thudded)
                        {
                            thudded = true;
                            PlayThud(contactInfo.NormalImpulse / ThudMaxImpulseForVolume);
                        }
                    }
                }
            }

            return totalImpulse;
        }
    }
}
