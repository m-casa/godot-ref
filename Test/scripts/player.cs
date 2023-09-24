using Godot;
using System;

public partial class player : CharacterBody3D
{
    // Don't forget to rebuild the project so the editor knows about the new export variables.

    #region FIELDS

    private float _gravity;
    private Vector3 _direction;
    private Vector3 _targetVelocity;
    private Node3D _springArmPivot;
    private Node3D _springArm;
    private AnimationPlayer _animationPlayer;
    private bool _landed;

    #endregion

    #region PROPERTIES

    /// <summary>
    /// How fast the player moves in meters per second.
    /// </summary>
    
    [Export]
    private int Speed { get; set; } = 5;

    /// <summary>
    /// The player's jump velocity.
    /// </summary>

    [Export]
    private int JumpVelocity { get; set; } = 5;

    #endregion

    #region METHODS

    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>

    public override void _Ready()
    {
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        _direction = Vector3.Zero;
        _targetVelocity = Vector3.Zero;

        _springArmPivot = GetNode<Node3D>("SpringArmPivot");
        _springArm = _springArmPivot.GetNode<Node3D>("SpringArm3D");

        _animationPlayer = GetNode<AnimationPlayer>("Pivot/GrapeBoi/AnimationPlayer");
        _animationPlayer.Play("Idle");
        _landed = true;

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <summary>
    /// Called when an Godot.InputEvent hasn't been consumed by Godot.Node._Input(Godot.InputEvent)
    ///  or any GUI Godot.Control item.
    /// </summary>

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion)
        {
            InputEventMouseMotion mouseMotion = (InputEventMouseMotion)inputEvent;
            _springArmPivot.RotateY(-mouseMotion.Relative.X * 0.001f);
            _springArm.RotateX(-mouseMotion.Relative.Y * 0.001f);

            Vector3 clamp = _springArm.Rotation;
            clamp.X = Mathf.Clamp(_springArm.Rotation.X, -Mathf.Pi / 4, Mathf.Pi / 4);
            _springArm.Rotation = clamp;
        }
    }

    /// <summary>
    /// Called during the physics processing step of the main loop.
    /// </summary>

    public override void _PhysicsProcess(double delta)
    {
        // Store the player's movement inputs as the direction.
        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        _direction = new Vector3(input.X, 0, input.Y).Normalized();

        RotatePlayer();

        Animate();

        AdjustVerticalVelocity(delta);

        AdjustGroundVelocity();

        MoveAndSlide();
    }

    /// <summary>
    /// Set the direction relative to the camera. Then rotate the player according to the direction.
    /// </summary>

    private void RotatePlayer()
    {
        // Rotate the player.
        if (_direction != Vector3.Zero)
        {
            _direction = _direction.Rotated(Vector3.Up, _springArmPivot.Rotation.Y);

            // Get the player's pivot point and transform.
            Node3D pivot = GetNode<Node3D>("Pivot");
            Transform3D pivotTransform = pivot.GlobalTransform;

            // Convert basis to quaternion, keep in mind scale is lost.
            Quaternion currentRotation = pivotTransform.Basis.GetRotationQuaternion();
            Quaternion targetRoation = pivotTransform.LookingAt(Position + _direction, Vector3.Up).Basis.GetRotationQuaternion();

            // Interpolate using spherical-linear interpolation (SLERP).
            Quaternion newRotation = currentRotation.Slerp(targetRoation, 0.15f); // Find halfway point between a and b.
                                                                                  // Apply back.
            pivot.Basis = new Basis(newRotation);
        }
    }

    /// <summary>
    /// Check the player's state and then animate their character.
    /// </summary>

    private void Animate()
    {
        if (_direction == Vector3.Zero && IsOnFloor())
        {
            if (!_landed)
            {
                _landed = true;
                _animationPlayer.Play("Land", 0.15f);
            }
            else if (_animationPlayer.CurrentAnimation != "Land")
            {
                _animationPlayer.Play("Idle", 0.15f);
            }
        }

        else if (_direction != Vector3.Zero && IsOnFloor())
        {
            _landed = true;
            _animationPlayer.Play("Run", 0.15f);
        }

        else if (_targetVelocity.Y > 0f && !IsOnFloor())
        {
            _animationPlayer.Play("Jump", 0.15f);
        }

        else if (_targetVelocity.Y < 0f && !IsOnFloor())
        {
            _animationPlayer.Play("Fall", 0.15f);
        }
    }

    /// <summary>
    /// Adjust the player's velocity while they're falling or jumping.
    /// </summary>

    private void AdjustVerticalVelocity(double delta)
    {
        // Add gravity.
        if (!IsOnFloor())
        {
            _landed = false;
            _targetVelocity.Y -= _gravity * (float)delta;
        }

        // Apply Jump velocity.
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            _targetVelocity.Y = JumpVelocity;
        }
    }

    /// <summary>
    /// Adjust the player's velocity and move them accordingly.
    /// </summary>

    private void AdjustGroundVelocity()
    {
        // Ground velocity.
        _targetVelocity.X = _direction.X * Speed;
        _targetVelocity.Z = _direction.Z * Speed;

        // Moving the player.
        Velocity = _targetVelocity;
    }

    #endregion
}
