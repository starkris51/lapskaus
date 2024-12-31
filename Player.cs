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
    private RayCast3D _rayCast;
    private Node3D _laserOrigin;
    private Vector3 _initialCameraPosition;

    MultiplayerSynchronizer multiplayerSynchronizer;

    [Export]
    private float interpolationSpeed = 15.0f;

    [Export]
    private Vector3 syncPos = Vector3.Zero;

    private PackedScene _bulletTrailScene;
    private Marker3D _marker;
    private const float TRAIL_LIFETIME = 0.1f;

    private AnimatedSprite2D gunSprite;

    private bool canShoot = true;

    private AudioStreamPlayer _shootSound;
    private AudioStreamPlayer _reloadSound;

    private Label killText;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _rayCast = GetNode<RayCast3D>("Camera3D/RayCast3D");
        _bulletTrailScene = ResourceLoader.Load<PackedScene>("res://ray_bullet.tscn");
        _marker = GetNode<Marker3D>("%RayStart");
        _shootSound = GetNode<AudioStreamPlayer>("%ShootSound");
        _reloadSound = GetNode<AudioStreamPlayer>("%ReloadSound");
        killText = GetNode<Label>("%KillText");
        killText.Visible = false;
        gunSprite = GetNode<AnimatedSprite2D>("%GunSprite");
        multiplayerSynchronizer = GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");
        multiplayerSynchronizer.SetMultiplayerAuthority(int.Parse(Name));

        AddToGroup("Players");

        Multiplayer.ServerDisconnected += OnServerDisconnected;
        gunSprite.AnimationFinished += OnGunAnimationFinished;
        gunSprite.Play("Idle");
        SetPhysicsProcess(IsMultiplayerAuthority());
        SetProcessInput(IsMultiplayerAuthority());

        if (IsMultiplayerAuthority())
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _camera.Current = true;
            _initialCameraPosition = _camera.Transform.Origin;
        }
        else
        {
            _camera.Current = false;
            gunSprite.Visible = false;
        }
    }

    private void OnServerDisconnected()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        EventManager.EmitLoadMap("res://server_menu.tscn");
    }

    public override void _ExitTree()
    {
        Multiplayer.ServerDisconnected -= OnServerDisconnected;
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }

        if (Input.IsActionJustPressed("shoot") && canShoot)
        {
            Rpc(nameof(Shoot));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority())
        {
            HandleMovement((float)delta);
            syncPos = GlobalPosition;
        }
        else
        {
            GlobalPosition = GlobalPosition.Lerp(syncPos, interpolationSpeed * (float)delta);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }
        if (@event is InputEventMouseMotion mouseEvent)
        {
            RotateY(-mouseEvent.Relative.X * MouseSensitivity);
            _camera.RotateX(-mouseEvent.Relative.Y * MouseSensitivity);
            Vector3 cameraRotation = _camera.Rotation;
            cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
            _camera.Rotation = cameraRotation;
        }
    }


    private void OnGunAnimationFinished()
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }

        switch (gunSprite.Animation)
        {
            case "Shoot":
                gunSprite.Play("Reload");
                _reloadSound.Play();
                break;
            case "Reload":
                gunSprite.Play("Idle");
                canShoot = true;
                killText.Visible = false;
                break;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Shoot()
    {
        gunSprite.Play("Shoot");
        if (IsMultiplayerAuthority())
        {
            _shootSound.Play();
        }
        canShoot = false;

        Vector2 screenCenter = GetViewport().GetVisibleRect().Size / 2;
        Vector3 rayOrigin = _camera.ProjectRayOrigin(screenCenter);
        Vector3 rayDirection = _camera.ProjectRayNormal(screenCenter) * 1000;

        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
        PhysicsRayQueryParameters3D rayParams = new PhysicsRayQueryParameters3D
        {
            From = rayOrigin,
            To = rayOrigin + rayDirection,
            CollideWithBodies = true,
            CollideWithAreas = true
        };

        var result = spaceState.IntersectRay(rayParams);

        if (result.Count > 0)
        {
            var hitCollider = result["collider"].As<Node>();
            if (hitCollider != null)
            {
                var hitNode = hitCollider;
                while (hitNode != null && !hitNode.IsInGroup("Players"))
                {
                    hitNode = hitNode.GetParent();
                }

                if (hitNode != null && hitNode != this && IsMultiplayerAuthority())
                {
                    killText.Visible = true;
                    killText.Text = "You are Killed the sigma named: " + GameManager.Players.Find(p => p.Id == int.Parse(hitNode.Name)).Name;
                    hitNode.Rpc(nameof(ServerHandleHit), int.Parse(hitNode.Name));
                }
            }
            CreateBulletTrail(_marker.GlobalPosition, rayOrigin + rayDirection);
        }
        else
        {
            CreateBulletTrail(_marker.GlobalPosition, rayOrigin + rayDirection);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void ServerHandleHit(int targetId)
    {
        if (!Multiplayer.IsServer())
            return;

        var targetNode = GetTree().CurrentScene.GetNode<Player>(targetId.ToString());
        if (targetNode != null)
        {
            targetNode.Rpc(nameof(Die), targetId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Die(int id)
    {
        if (id == int.Parse(Name))
        {
            GlobalPosition = new Vector3(0, 5, 0);
        }
    }
    private void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        Node3D bulletTrail = _bulletTrailScene.Instantiate<Node3D>();
        AddChild(bulletTrail);

        bulletTrail.GlobalPosition = start;
        float length = start.DistanceTo(end);
        bulletTrail.LookAt(end);
        bulletTrail.Rotate(Vector3.Right, Mathf.Pi / 2);
        bulletTrail.Scale = new Vector3(0.02f, length / 2, 0.02f);

        SceneTreeTimer timer = GetTree().CreateTimer(TRAIL_LIFETIME);
        timer.Timeout += () => bulletTrail.QueueFree();
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
        _camera.Transform = cameraTransform;

        // float targetTilt = 0.0f;
        // if (Input.IsActionPressed("move_left")) targetTilt = TiltAmount;
        // else if (Input.IsActionPressed("move_right")) targetTilt = -TiltAmount;

        // float currentTilt = cameraTransform.Basis.GetEuler().Z;
        // float newTilt = Mathf.Lerp(currentTilt, targetTilt, TiltSpeed * delta);

        // Vector3 cameraEuler = cameraTransform.Basis.GetEuler();
        // cameraEuler.Z = newTilt;
        // cameraTransform.Basis = Basis.Identity.Rotated(Vector3.Forward, newTilt);

        MoveAndSlide();
    }

    private Vector3 Headbob(float time)
    {
        return new Vector3(Mathf.Sin(time * Headbob_Freq / 2) * Headbob_Amp, Mathf.Sin(time * Headbob_Freq) * Headbob_Amp, 0);
    }

}
