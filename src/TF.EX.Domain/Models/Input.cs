﻿using MessagePack;
using System.Runtime.InteropServices;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.Domain.Models
{
    [StructLayout(LayoutKind.Sequential)]
    [MessagePackObject]
    public struct Input
    {
        [Key(0)]
        public int jump_check { get; set; }

        [Key(1)]
        public int jump_pressed { get; set; }

        [Key(2)]
        public int shoot_check { get; set; }

        [Key(3)]
        public int shoot_pressed { get; set; }

        [Key(4)]
        public int alt_shoot_check { get; set; }

        [Key(5)]
        public int alt_shoot_pressed { get; set; }

        [Key(6)]
        public int dodge_check { get; set; }

        [Key(7)]
        public int dodge_pressed { get; set; }

        [Key(8)]
        public int arrow_pressed { get; set; }

        [Key(9)]
        public int move_x { get; set; }

        [Key(10)]
        public int move_y { get; set; }

        [Key(11)]
        public Vector2f aim_axis { get; set; }

        [Key(12)]
        public Vector2f aim_right_axis { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Inputs
    {
        public IntPtr data;
        public int len;
    }

    public class InputsImpl : IDisposable
    {
        public List<Input> _inputs { get; internal set; }

        public InputsHandle Handle { get; internal set; }

        public InputsImpl(Inputs inputs)
        {
            Handle = new InputsHandle(inputs);

            unsafe
            {
                byte[] buffer = new byte[inputs.len * Marshal.SizeOf(typeof(Input))];
                Marshal.Copy(inputs.data, buffer, 0, buffer.Length);

                Input[] inputsArray = new Input[inputs.len];
                fixed (byte* pBuffer = buffer)
                {
                    for (int i = 0; i < inputs.len; i++)
                    {
                        int offset = i * Marshal.SizeOf(typeof(Input));
                        IntPtr structPtr = new IntPtr(pBuffer + offset);
                        inputsArray[i] = (Input)Marshal.PtrToStructure(structPtr, typeof(Input));
                    }
                }

                _inputs = inputsArray.ToList();
            }
        }

        public void Dispose()
        {
            Handle.Dispose();
        }
    }

    public class InputsHandle : SafeHandle
    {
        private Inputs _inputs;
        public InputsHandle(Inputs inputs) : base(IntPtr.Zero, true)
        {
            SetHandle(inputs.data);
            _inputs = inputs;
        }

        public override bool IsInvalid
        {
            get { return this.handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
            {
                GGRSFFI.netplay_inputs_free(_inputs);
            }

            return true;
        }
    }

    public static class InputExtensions
    {
        public static InputsImpl ToModel(this Inputs inputs) => new InputsImpl(inputs);

        public static int ToInt(this bool value) => value ? 1 : 0;
        public static bool ToBool(this int value) => value == 1;

        public static InputState ToTFInput(this Input input)
        {
            return new InputState
            {
                AimAxis = input.aim_axis.ToTFVector(),
                AltShootCheck = input.alt_shoot_check.ToBool(),
                AltShootPressed = input.alt_shoot_pressed.ToBool(),
                ArrowsPressed = input.arrow_pressed.ToBool(),
                DodgeCheck = input.dodge_check.ToBool(),
                DodgePressed = input.dodge_pressed.ToBool(),
                JumpCheck = input.jump_check.ToBool(),
                JumpPressed = input.jump_pressed.ToBool(),
                MoveX = input.move_x,
                MoveY = input.move_y,
                ShootCheck = input.shoot_check.ToBool(),
                ShootPressed = input.shoot_pressed.ToBool()
            };
        }
    }
}
