using System.Runtime.InteropServices;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.Domain.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        public bool jump_check { get; set; }

        public bool jump_pressed { get; set; }

        public bool shoot_check { get; set; }

        public bool shoot_pressed { get; set; }

        public bool alt_shoot_check { get; set; }

        public bool alt_shoot_pressed { get; set; }

        public bool dodge_check { get; set; }

        public bool dodge_pressed { get; set; }

        public bool arrow_pressed { get; set; }

        public int move_x { get; set; }

        public int move_y { get; set; }

        public Vector2f aim_axis;
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

        public static Input ToModel(this InputState input)
        {
            return new Input
            {
                aim_axis = input.AimAxis.ToModel(),
                alt_shoot_check = input.AltShootCheck,
                alt_shoot_pressed = input.AltShootPressed,
                arrow_pressed = input.ArrowsPressed,
                dodge_check = input.DodgeCheck,
                dodge_pressed = input.DodgePressed,
                jump_check = input.JumpCheck,
                jump_pressed = input.JumpPressed,
                move_x = input.MoveX,
                move_y = input.MoveY,
                shoot_check = input.ShootCheck,
                shoot_pressed = input.ShootPressed
            };
        }

        public static InputState ToTFInput(this Input input)
        {
            return new InputState
            {
                AimAxis = input.aim_axis.ToTFVector(),
                AltShootCheck = input.alt_shoot_check,
                AltShootPressed = input.alt_shoot_pressed,
                ArrowsPressed = input.arrow_pressed,
                DodgeCheck = input.dodge_check,
                DodgePressed = input.dodge_pressed,
                JumpCheck = input.jump_check,
                JumpPressed = input.jump_pressed,
                MoveX = input.move_x,
                MoveY = input.move_y,
                ShootCheck = input.shoot_check,
                ShootPressed = input.shoot_pressed
            };
        }
    }
}
