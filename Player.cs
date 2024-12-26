using Godot;
using System;
using System.Runtime.InteropServices;

public partial class Player : CharacterBody3D
{
    [Export] private float MoveSpeed = 5.0f;
    [Export] private float MouseSensitivity = 0.01f;
    [Export] private float JumpStrength = 5.0f;
    [Export] private float Gravity = 9f;
    [Export] private float Headbob_Freq = 2.0f;
    [Export] private float Headbob_Amp = 0.08f;

    [Export] private float TiltAmount = 0.05f;
    [Export] private float TiltSpeed = 10.0f;

    private float Headbob_t = 0.0f;

    private Vector2 _mouseDelta = Vector2.Zero;

    private Vector3 _velocity = Vector3.Zero;
    private Camera3D _camera;
    private Vector3 _initialCameraPosition;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _initialCameraPosition = _camera.Transform.Origin;

    }

    public override void _PhysicsProcess(double delta)
    {
        HandleMovement((float)delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {
            RotateY(-mouseEvent.Relative.X * MouseSensitivity);
            _camera.RotateX(-mouseEvent.Relative.Y * MouseSensitivity);
            Vector3 cameraRotation = _camera.Rotation;
            cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-40), Mathf.DegToRad(60));
            _camera.Rotation = cameraRotation;
        }
    }

    private void HandleMovement(float delta)
    {
        Vector3 direction = Vector3.Zero;

        if (Input.IsActionPressed("move_forward")) direction -= Transform.Basis.Z;
        if (Input.IsActionPressed("move_backward")) direction += Transform.Basis.Z;
        if (Input.IsActionPressed("move_left")) direction -= Transform.Basis.X;
        if (Input.IsActionPressed("move_right")) direction += Transform.Basis.X;

        direction = direction.Normalized();

        Vector3 horizontalVelocity = direction * MoveSpeed;

        if (!IsOnFloor())
        {
            _velocity.Y -= Gravity * delta;
        }
        else if (Input.IsActionJustPressed("jump"))
        {
            _velocity.Y = JumpStrength;
        }
        else
        {
            _velocity.Y = 0;
        }

        _velocity.X = horizontalVelocity.X;
        _velocity.Z = horizontalVelocity.Z;

        Velocity = _velocity;

        Headbob_t += delta * _velocity.Length() * (float)(IsOnFloor() ? 1.0f : 0.0f);
        Transform3D cameraTransform = _camera.Transform;
        cameraTransform.Origin = _initialCameraPosition + Headbob(Headbob_t);

        // float targetTilt = 0.0f;
        // if (Input.IsActionPressed("move_left")) targetTilt = TiltAmount;
        // else if (Input.IsActionPressed("move_right")) targetTilt = -TiltAmount;

        // float currentTilt = cameraTransform.Basis.GetEuler().Z;
        // float newTilt = Mathf.Lerp(currentTilt, targetTilt, TiltSpeed * delta);

        // Vector3 cameraEuler = cameraTransform.Basis.GetEuler();
        // cameraEuler.Z = newTilt;
        // cameraTransform.Basis = Basis.Identity.Rotated(Vector3.Forward, newTilt);

        _camera.Transform = cameraTransform;
        MoveAndSlide();
    }

    private Vector3 Headbob(float time)
    {
        return new Vector3(Mathf.Sin(time * Headbob_Freq / 2) * Headbob_Amp, Mathf.Sin(time * Headbob_Freq) * Headbob_Amp, 0);
    }
}
