/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using SharpLife.Engine.Entities;
using SharpLife.Engine.Entities.Components;
using SharpLife.Engine.Physics;
using System.Numerics;

namespace SharpLife.Game.Entities.Components
{
    /// <summary>
    /// The local player.
    /// <remarks>
    /// The engine has no full player physics yet, so this is a simple walk controller:
    /// horizontal movement, gravity, jumping and collision handling using the engine's tracing code.
    /// Movement follows GoldSource conventions (Z up, inputs relative to view yaw).
    /// </remarks>
    /// </summary>
    public sealed class Player : PlayerController
    {
        /// <summary>
        /// GoldSource player hull
        /// </summary>
        public static readonly Vector3 HullMins = new Vector3(-16, -16, -36);

        public static readonly Vector3 HullMaxs = new Vector3(16, 16, 36);

        /// <summary>
        /// Height of the eyes above the entity origin (VEC_VIEW in the SDK)
        /// </summary>
        public const float EyeHeight = 28f;

        public float MoveSpeed = 320f;

        public float JumpSpeed = 268f;

        public float Gravity = 800f;

        public Vector3 Velocity;

        public bool OnGround;

        private Vector3? _spawnOrigin;

        private bool _spawnUnstuckDone;

        private float _nextLogTime;

        //AUTOTEST: seconds of automatic forward walking
        private float _autoWalkTimer;

        //AUTOTEST: enabled by SHARPLIFE_AUTOWALK=1, drives the player so collisions can be tested
        //without synthetic key input (SDL ignores it).
        private readonly bool _autoWalk = System.Environment.GetEnvironmentVariable("SHARPLIFE_AUTOWALK") == "1";


        public override Vector3 EyePosition
        {
            get
            {
                var transform = Entity.GetComponent<Transform>();

                return (transform?.Origin ?? Vector3.Zero) + new Vector3(0, 0, EyeHeight);
            }
        }

        public override void Move(Scene world, float deltaSeconds, in Vector3 forward, in Vector3 right, float forwardMove, float sideMove, bool jump)
        {
            var transform = Entity.GetComponent<Transform>();
            var collider = Entity.GetComponent<Collider>();

            if (world == null || transform == null || collider == null || deltaSeconds <= 0)
            {
                return;
            }

            var wishDirection = forward * forwardMove + right * sideMove;

            //AUTOTEST: drive the player in all four directions so collisions can be verified
            //without relying on synthetic key input, which SDL ignores.
            if (_autoWalk)
            {
                _autoWalkTimer += deltaSeconds;

                if (_autoWalkTimer < 4.0f)
                {
                    wishDirection = Vector3.UnitX;
                }
                else if (_autoWalkTimer < 8.0f)
                {
                    wishDirection = -Vector3.UnitX;
                }
                else if (_autoWalkTimer < 12.0f)
                {
                    wishDirection = Vector3.UnitY;
                }
                else if (_autoWalkTimer < 16.0f)
                {
                    wishDirection = -Vector3.UnitY;
                }
            }

            if (wishDirection.LengthSquared() > 1f)
            {
                wishDirection = Vector3.Normalize(wishDirection);
            }

            //TODO: proper acceleration, friction and air control
            Velocity.X = wishDirection.X * MoveSpeed;
            Velocity.Y = wishDirection.Y * MoveSpeed;

            if (OnGround)
            {
                if (jump)
                {
                    Velocity.Z = JumpSpeed;
                    OnGround = false;
                }
                else if (Velocity.Z < 0)
                {
                    Velocity.Z = 0;
                }
            }
            else
            {
                Velocity.Z -= Gravity * deltaSeconds;
            }

            var start = transform.Origin;

            //Autotest mode logs the position once a second so collision checks can be verified
            //without synthetic key input.
            if (_autoWalk && world.Time.ElapsedTime >= _nextLogTime)
            {
                _nextLogTime = (float)world.Time.ElapsedTime + 1.0f;

                world.Logger.Information("Player move: origin={Origin} velocity={Velocity} onGround={OnGround} engineTime={Time}",
                    start, Velocity, OnGround, world.Time.ElapsedTime);
            }

            //Falling out of the world is not handled yet (the engine has no triggers or fall damage),
            //so put the player back at the spawn point instead of falling forever.
            var mapModel = world.WorldState.MapInfo?.Model;

            if (mapModel != null && (start.Z < mapModel.Mins.Z - 512.0f || start.Z > mapModel.Maxs.Z + 512.0f))
            {
                if (_spawnOrigin == null)
                {
                    _spawnOrigin = start + new Vector3(0, 0, 512.0f);
                }

                transform.Origin = _spawnOrigin.Value;

                Velocity = Vector3.Zero;

                world.Logger.Information("Player fell out of the world, returning to spawn {Origin}", _spawnOrigin.Value);

                return;
            }

            if (_spawnOrigin == null)
            {
                _spawnOrigin = start;
            }

            //Map spawn points sit on the floor, which leaves the player hull embedded in geometry
            //(traces report StartSolid and the player falls through the world). Push the hull straight
            //up until it's free, the same way the original engine unsticks players.
            //Only done once: outside the map every trace reports StartSolid, and lifting the player
            //every frame would launch it straight up.
            if (!_spawnUnstuckDone)
            {
                for (var attempt = 0; attempt < 24; ++attempt)
                {
                    var test = world.Physics.Move(start, collider.Mins, collider.Maxs, start, TraceType.None, collider, false, false);

                    if (!test.StartSolid)
                    {
                        break;
                    }

                    start.Z += 8.0f;
                }

                transform.Origin = start;

                _spawnUnstuckDone = true;
            }

            var end = start + Velocity * deltaSeconds;

            var trace = world.Physics.Move(start, collider.Mins, collider.Maxs, end, TraceType.None, collider, false, false);

            //Try to step over low obstacles (stairs, ledges) like the original engine does
            if (trace.Fraction < 1.0f && Velocity.Z <= 0.0f)
            {
                const float stepSize = 18.0f;

                var raisedStart = start + new Vector3(0, 0, stepSize);
                var raisedTrace = world.Physics.Move(start, collider.Mins, collider.Maxs, raisedStart, TraceType.None, collider, false, false);

                if (!raisedTrace.StartSolid)
                {
                    var forwardEnd = raisedTrace.EndPosition + (end - start);
                    var forwardTrace = world.Physics.Move(raisedTrace.EndPosition, collider.Mins, collider.Maxs, forwardEnd, TraceType.None, collider, false, false);

                    if (forwardTrace.Fraction > trace.Fraction)
                    {
                        var landEnd = forwardTrace.EndPosition - new Vector3(0, 0, stepSize);
                        var landTrace = world.Physics.Move(forwardTrace.EndPosition, collider.Mins, collider.Maxs, landEnd, TraceType.None, collider, false, false);

                        if (!landTrace.StartSolid && landTrace.Fraction > 0.0f)
                        {
                            trace = landTrace;
                            trace.EndPosition = landTrace.EndPosition;
                            OnGround = true;
                            Velocity.Z = 0;
                        }
                    }
                }
            }

            transform.Origin = trace.EndPosition;

            if (trace.StartSolid || trace.Fraction < 1f)
            {
                var normal = trace.Plane.Normal;

                //Remove the velocity component that pushes into the surface
                var dot = Vector3.Dot(Velocity, normal);

                if (dot < 0)
                {
                    Velocity -= normal * dot;
                }

                if (normal.Z > 0.7f && Velocity.Z <= 0)
                {
                    OnGround = true;
                }
            }

            //Probe the ground so slopes and small steps are detected
            var groundTrace = world.Physics.Move(transform.Origin, collider.Mins, collider.Maxs,
                transform.Origin - new Vector3(0, 0, 2), TraceType.None, collider, false, false);

            if (groundTrace.Fraction < 1f && groundTrace.Plane.Normal.Z >= 0.7f)
            {
                OnGround = true;

                if (Velocity.Z < 0)
                {
                    Velocity.Z = 0;
                }
            }
            else
            {
                //Nothing under our feet: start falling instead of gliding along at the current height
                OnGround = false;
            }
        }
    }
}
