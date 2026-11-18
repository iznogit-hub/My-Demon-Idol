using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    [Flags]
    public enum InputButton
    {
        None        = 0,
        Light       = 1 << 0,
        Medium      = 1 << 1,
        Heavy       = 1 << 2,
        DriveSkill  = 1 << 3,
        JumpAttack  = 1 << 4,
        Backdash    = 1 << 5,
        Parry       = 1 << 6,
        DomainSuper = 1 << 7,
        Burst       = 1 << 8,
        Barrier     = 1 << 9,
        SidestepUp  = 1 << 10,
        SidestepDown= 1 << 11,
        ForwardDash = 1 << 12,
        Throw       = 1 << 13
    }

    public struct BufferedCommand
    {
        public InputButton Button;
        public ThreeButtonCommand ThreeBtnCommand;
        public int TickTimestamp;
        public Vector2 Direction;

        public BufferedCommand(InputButton button, int tick, Vector2 direction)
        {
            Button = button;
            ThreeBtnCommand = (button == InputButton.Light) ? ThreeButtonCommand.Light
                            : (button == InputButton.Heavy) ? ThreeButtonCommand.Heavy
                            : (button == InputButton.DriveSkill) ? ThreeButtonCommand.Skill
                            : (button == InputButton.Throw) ? ThreeButtonCommand.Throw
                            : (button == InputButton.DomainSuper) ? ThreeButtonCommand.Super
                            : (button == InputButton.Burst) ? ThreeButtonCommand.Burst
                            : ThreeButtonCommand.None;
            TickTimestamp = tick;
            Direction = direction;
        }

        public BufferedCommand(ThreeButtonCommand cmd, int tick, Vector2 direction)
        {
            ThreeBtnCommand = cmd;
            Button = (cmd == ThreeButtonCommand.Light) ? InputButton.Light
                   : (cmd == ThreeButtonCommand.Heavy) ? InputButton.Heavy
                   : (cmd == ThreeButtonCommand.Skill) ? InputButton.DriveSkill
                   : (cmd == ThreeButtonCommand.Throw) ? InputButton.Throw
                   : (cmd == ThreeButtonCommand.Super) ? InputButton.DomainSuper
                   : (cmd == ThreeButtonCommand.Burst) ? InputButton.Burst
                   : InputButton.None;
            TickTimestamp = tick;
            Direction = direction;
        }
    }

    /// <summary>
    /// Frame-stamped ring buffer for 3-button 2.5D combat inputs.
    /// Default leniency is 5 actionable ticks with one-time consumption.
    /// Freezes command age during hitstop and superflash; clears cleanly on round resets.
    /// </summary>
    public class InputBuffer
    {
        public const int DEFAULT_LENIENCY_TICKS = 5;
        private const int BUFFER_SIZE = 16;
        private readonly BufferedCommand[] _buffer = new BufferedCommand[BUFFER_SIZE];
        private int _head = 0;
        private readonly int _leniencyTicks;

        public InputBuffer(int leniencyTicks = DEFAULT_LENIENCY_TICKS)
        {
            _leniencyTicks = leniencyTicks;
        }

        public void Enqueue(InputButton button, int currentTick, Vector2 direction)
        {
            if (button == InputButton.None) return;

            _head = (_head + 1) % BUFFER_SIZE;
            _buffer[_head] = new BufferedCommand(button, currentTick, direction);
        }

        public void Enqueue(ThreeButtonCommand cmd, int currentTick, Vector2 direction)
        {
            if (cmd == ThreeButtonCommand.None) return;

            _head = (_head + 1) % BUFFER_SIZE;
            _buffer[_head] = new BufferedCommand(cmd, currentTick, direction);
        }

        public bool PeekLatest(out BufferedCommand cmd)
        {
            cmd = _buffer[_head];
            return cmd.Button != InputButton.None || cmd.ThreeBtnCommand != ThreeButtonCommand.None;
        }

        public bool HasCommand(InputButton button, int currentTick, out BufferedCommand command)
        {
            return HasCommand(button, currentTick, out command, _leniencyTicks);
        }

        public bool HasCommand(InputButton button, int currentTick, out BufferedCommand command, int customLeniencyTicks)
        {
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                int idx = (_head - i + BUFFER_SIZE) % BUFFER_SIZE;
                BufferedCommand cmd = _buffer[idx];

                if (cmd.Button.HasFlag(button) && (cmd.Button != InputButton.None))
                {
                    if (currentTick - cmd.TickTimestamp <= customLeniencyTicks)
                    {
                        command = cmd;
                        return true;
                    }
                }
            }

            command = default;
            return false;
        }

        public bool HasCommand(ThreeButtonCommand cmd, int currentTick, out BufferedCommand command, int customLeniencyTicks = DEFAULT_LENIENCY_TICKS)
        {
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                int idx = (_head - i + BUFFER_SIZE) % BUFFER_SIZE;
                BufferedCommand bCmd = _buffer[idx];

                if (bCmd.ThreeBtnCommand == cmd && cmd != ThreeButtonCommand.None)
                {
                    if (currentTick - bCmd.TickTimestamp <= customLeniencyTicks)
                    {
                        command = bCmd;
                        return true;
                    }
                }
            }

            command = default;
            return false;
        }

        public void Consume(InputButton button)
        {
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                int idx = (_head - i + BUFFER_SIZE) % BUFFER_SIZE;
                if (_buffer[idx].Button.HasFlag(button))
                {
                    _buffer[idx].Button &= ~button;
                    _buffer[idx].ThreeBtnCommand = ThreeButtonCommand.None;
                    break;
                }
            }
        }

        public void Consume(ThreeButtonCommand cmd)
        {
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                int idx = (_head - i + BUFFER_SIZE) % BUFFER_SIZE;
                if (_buffer[idx].ThreeBtnCommand == cmd)
                {
                    _buffer[idx].ThreeBtnCommand = ThreeButtonCommand.None;
                    _buffer[idx].Button = InputButton.None;
                    break;
                }
            }
        }

        /// <summary>
        /// Freezes age of buffered commands during authored hitstop/superflash
        /// by pushing their timestamp forward by the freeze duration.
        /// </summary>
        public void FreezeAge(int freezeTicks)
        {
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (_buffer[i].ThreeBtnCommand != ThreeButtonCommand.None || _buffer[i].Button != InputButton.None)
                {
                    _buffer[i].TickTimestamp += freezeTicks;
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                _buffer[i] = default;
            }
        }
    }
}
