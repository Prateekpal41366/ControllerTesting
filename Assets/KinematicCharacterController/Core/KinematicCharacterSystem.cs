using System.Collections.Generic;
using UnityEngine;

namespace KinematicCharacterController
{
    [DefaultExecutionOrder(-100)]
    public class KinematicCharacterSystem : MonoBehaviour
    {
        private static KinematicCharacterSystem _instance;

        public static List<KinematicCharacterMotor> CharacterMotors = new List<KinematicCharacterMotor>();
        public static List<PhysicsMover> PhysicsMovers = new List<PhysicsMover>();

        private static float _lastCustomInterpolationStartTime = -1f;
        private static float _lastCustomInterpolationDeltaTime = -1f;

        public static KCCSettings Settings;

        #region Safe Quaternion

        private static Quaternion SafeNormalize(Quaternion q)
        {
            float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);

            if (mag > 0.0001f)
            {
                float inv = 1f / mag;
                q.x *= inv;
                q.y *= inv;
                q.z *= inv;
                q.w *= inv;
                return q;
            }

            return Quaternion.identity;
        }

        #endregion

        #region Singleton Setup

        public static void EnsureCreation()
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("KinematicCharacterSystem");
                _instance = go.AddComponent<KinematicCharacterSystem>();

                go.hideFlags = HideFlags.NotEditable;
                _instance.hideFlags = HideFlags.NotEditable;

                Settings = ScriptableObject.CreateInstance<KCCSettings>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            Destroy(gameObject);
        }

        public static KinematicCharacterSystem GetInstance()
        {
            return _instance;
        }

        #endregion

        #region Registration

        public static void RegisterCharacterMotor(KinematicCharacterMotor motor)
        {
            CharacterMotors.Add(motor);
        }

        public static void UnregisterCharacterMotor(KinematicCharacterMotor motor)
        {
            CharacterMotors.Remove(motor);
        }

        public static void RegisterPhysicsMover(PhysicsMover mover)
        {
            PhysicsMovers.Add(mover);
            mover.Rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        public static void UnregisterPhysicsMover(PhysicsMover mover)
        {
            PhysicsMovers.Remove(mover);
        }

        #endregion

        #region Unity Loop

        private void FixedUpdate()
        {
            if (!Settings.AutoSimulation)
                return;

            float dt = Time.deltaTime;

            if (Settings.Interpolate)
                PreSimulationInterpolationUpdate(dt);

            Simulate(dt, CharacterMotors, PhysicsMovers);

            if (Settings.Interpolate)
                PostSimulationInterpolationUpdate(dt);
        }

        private void LateUpdate()
        {
            if (Settings.Interpolate)
                CustomInterpolationUpdate();
        }

        #endregion

        #region Simulation

        public static void PreSimulationInterpolationUpdate(float deltaTime)
        {
            foreach (var motor in CharacterMotors)
            {
                motor.InitialTickPosition = motor.TransientPosition;
                motor.InitialTickRotation = SafeNormalize(motor.TransientRotation);

                motor.Transform.SetPositionAndRotation(
                    motor.TransientPosition,
                    motor.InitialTickRotation);
            }

            foreach (var mover in PhysicsMovers)
            {
                mover.InitialTickPosition = mover.TransientPosition;
                mover.InitialTickRotation = SafeNormalize(mover.TransientRotation);

                mover.Transform.SetPositionAndRotation(
                    mover.TransientPosition,
                    mover.InitialTickRotation);

                mover.Rigidbody.position = mover.TransientPosition;
                mover.Rigidbody.rotation = mover.InitialTickRotation;
            }
        }

        public static void Simulate(float deltaTime,
            List<KinematicCharacterMotor> motors,
            List<PhysicsMover> movers)
        {
            //this is running all the time//

            foreach (var mover in movers)
            {
                mover.VelocityUpdate(deltaTime);
            };

            foreach (var motor in motors)
                motor.UpdatePhase1(deltaTime);

            foreach (var mover in movers)
            {
                Quaternion rot = SafeNormalize(mover.TransientRotation);

                mover.Transform.SetPositionAndRotation(
                    mover.TransientPosition,
                    rot);

                mover.Rigidbody.position = mover.TransientPosition;
                mover.Rigidbody.rotation = rot;
            }

            foreach (var motor in motors)
            {
                motor.UpdatePhase2(deltaTime);

                motor.Transform.SetPositionAndRotation(
                    motor.TransientPosition,
                    SafeNormalize(motor.TransientRotation));
            }
        }

        public static void PostSimulationInterpolationUpdate(float deltaTime)
        {
            _lastCustomInterpolationStartTime = Time.time;
            _lastCustomInterpolationDeltaTime = deltaTime;

            foreach (var motor in CharacterMotors)
            {
                motor.Transform.SetPositionAndRotation(
                    motor.InitialTickPosition,
                    SafeNormalize(motor.InitialTickRotation));
            }

            foreach (var mover in PhysicsMovers)
            {
                Quaternion initialRot = SafeNormalize(mover.InitialTickRotation);
                Quaternion targetRot = SafeNormalize(mover.TransientRotation);

                if (mover.MoveWithPhysics)
                {
                    mover.Rigidbody.position = mover.InitialTickPosition;
                    mover.Rigidbody.rotation = initialRot;

                    mover.Rigidbody.MovePosition(mover.TransientPosition);
                    mover.Rigidbody.MoveRotation(targetRot);
                }
                else
                {
                    mover.Rigidbody.position = mover.TransientPosition;
                    mover.Rigidbody.rotation = targetRot;
                }
            }
        }

        private static void CustomInterpolationUpdate()
        {
            float factor = Mathf.Clamp01(
                (Time.time - _lastCustomInterpolationStartTime) /
                _lastCustomInterpolationDeltaTime);

            foreach (var motor in CharacterMotors)
            {
                motor.Transform.SetPositionAndRotation(
                    Vector3.Lerp(motor.InitialTickPosition,
                                 motor.TransientPosition,
                                 factor),
                    SafeNormalize(
                        Quaternion.Slerp(
                            motor.InitialTickRotation,
                            motor.TransientRotation,
                            factor)));
            }

            foreach (var mover in PhysicsMovers)
            {
                Vector3 newPos = Vector3.Lerp(
                    mover.InitialTickPosition,
                    mover.TransientPosition,
                    factor);

                Quaternion newRot = SafeNormalize(
                    Quaternion.Slerp(
                        mover.InitialTickRotation,
                        mover.TransientRotation,
                        factor));

                mover.Transform.SetPositionAndRotation(newPos, newRot);

                mover.PositionDeltaFromInterpolation =
                    newPos - mover.LatestInterpolationPosition;

                mover.RotationDeltaFromInterpolation =
                    Quaternion.Inverse(mover.LatestInterpolationRotation) * newRot;

                mover.LatestInterpolationPosition = newPos;
                mover.LatestInterpolationRotation = newRot;
            }
        }

        #endregion
    }
}