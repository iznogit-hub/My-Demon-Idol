#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// InputTestFixture implementation for editor and test runner environments.
    /// Provides Press, Release, and Set helpers using InputSystem.QueueEvent.
    /// </summary>
    public class InputTestFixture
    {
        public virtual void Setup()
        {
        }

        public virtual void TearDown()
        {
        }

        public void Set<TValue>(InputControl<TValue> control, TValue state) where TValue : struct
        {
            if (control == null) throw new ArgumentNullException(nameof(control));

            using (DeltaStateEvent.From(control, out var eventPtr))
            {
                control.WriteValueIntoEvent(state, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
            InputSystem.Update();
        }

        public void Press(ButtonControl button)
        {
            Set(button, 1f);
        }

        public void Release(ButtonControl button)
        {
            Set(button, 0f);
        }
    }
}
#endif
