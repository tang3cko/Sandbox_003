namespace ArenaSurvivor;

using Godot;

public partial class PlayerAnimator : Node
{
    private AnimationPlayer _animPlayer;
    private AnimationTree _animTree;
    private AnimationNodeStateMachinePlayback _stateMachine;
    private PlayerController _player;

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();

        _animPlayer = new AnimationPlayer();
        _animPlayer.Name = "AnimationPlayer";
        AddChild(_animPlayer);

        CreateAnimations();

        _animTree = new AnimationTree();
        _animTree.Name = "AnimationTree";
        var rootStateMachine = CreateStateMachine();
        _animTree.TreeRoot = rootStateMachine;
        _animTree.AnimPlayer = _animPlayer.GetPath();
        AddChild(_animTree);
        _animTree.Active = true;

        _stateMachine = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
        _stateMachine.Travel("idle");
    }

    public override void _Process(double delta)
    {
        if (_stateMachine == null) return;

        var current = _stateMachine.GetCurrentNode();

        if (_player.IsAttacking && current != "attack")
        {
            _stateMachine.Travel("attack");
        }
        else if (_player.IsDodging && current != "dodge")
        {
            _stateMachine.Travel("dodge");
        }
        else if (!_player.IsAttacking && !_player.IsDodging)
        {
            var vel = _player.Velocity;
            var speed = new Vector2(vel.X, vel.Z).Length();

            if (speed > 0.5f && current != "walk")
                _stateMachine.Travel("walk");
            else if (speed <= 0.5f && current != "idle")
                _stateMachine.Travel("idle");
        }
    }

    private void CreateAnimations()
    {
        _animPlayer.AddAnimationLibrary("", new AnimationLibrary());
        CreateResetAnimation();
        CreateIdleAnimation();
        CreateWalkAnimation();
        CreateAttackAnimation();
        CreateDodgeAnimation();
    }

    private void AddDefaultTracks(Animation anim)
    {
        // Every animation must include ALL animated properties to prevent
        // AnimationTree from resetting missing properties to zero (Godot #80971).
        AddConstantTrack(anim, "../MeshInstance3D:position", new Vector3(0, 0.9f, 0));
        AddConstantTrack(anim, "../MeshInstance3D:rotation", Vector3.Zero);
        AddConstantTrack(anim, "../MeshInstance3D:scale", Vector3.One);
        AddConstantTrack(anim, "../WeaponPivot:rotation", Vector3.Zero);
    }

    private void AddConstantTrack(Animation anim, string path, Variant value)
    {
        int track = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(track, path);
        anim.TrackInsertKey(track, 0.0f, value);
    }

    private void CreateResetAnimation()
    {
        var anim = new Animation();
        anim.Length = 0.001f;
        AddDefaultTracks(anim);
        _animPlayer.GetAnimationLibrary("").AddAnimation("RESET", anim);
    }

    private void CreateIdleAnimation()
    {
        var anim = new Animation();
        anim.Length = 1.2f;
        anim.LoopMode = Animation.LoopModeEnum.Linear;

        // Body bob (breathing)
        int posTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(posTrack, "../MeshInstance3D:position");
        anim.TrackInsertKey(posTrack, 0.0f, new Vector3(0, 0.9f, 0));
        anim.TrackInsertKey(posTrack, 0.6f, new Vector3(0, 0.93f, 0));
        anim.TrackInsertKey(posTrack, 1.2f, new Vector3(0, 0.9f, 0));

        // Hold defaults for other properties
        AddConstantTrack(anim, "../MeshInstance3D:rotation", Vector3.Zero);
        AddConstantTrack(anim, "../MeshInstance3D:scale", Vector3.One);
        AddConstantTrack(anim, "../WeaponPivot:rotation", Vector3.Zero);

        _animPlayer.GetAnimationLibrary("").AddAnimation("idle", anim);
    }

    private void CreateWalkAnimation()
    {
        var anim = new Animation();
        anim.Length = 0.5f;
        anim.LoopMode = Animation.LoopModeEnum.Linear;

        // Body bob
        int bodyTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(bodyTrack, "../MeshInstance3D:position");
        anim.TrackInsertKey(bodyTrack, 0.0f, new Vector3(0, 0.9f, 0));
        anim.TrackInsertKey(bodyTrack, 0.125f, new Vector3(0, 0.98f, 0));
        anim.TrackInsertKey(bodyTrack, 0.25f, new Vector3(0, 0.9f, 0));
        anim.TrackInsertKey(bodyTrack, 0.375f, new Vector3(0, 0.98f, 0));
        anim.TrackInsertKey(bodyTrack, 0.5f, new Vector3(0, 0.9f, 0));

        // Body tilt
        int tiltTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(tiltTrack, "../MeshInstance3D:rotation");
        anim.TrackInsertKey(tiltTrack, 0.0f, new Vector3(0, 0, -0.05f));
        anim.TrackInsertKey(tiltTrack, 0.25f, new Vector3(0, 0, 0.05f));
        anim.TrackInsertKey(tiltTrack, 0.5f, new Vector3(0, 0, -0.05f));

        // Hold defaults
        AddConstantTrack(anim, "../MeshInstance3D:scale", Vector3.One);
        AddConstantTrack(anim, "../WeaponPivot:rotation", Vector3.Zero);

        _animPlayer.GetAnimationLibrary("").AddAnimation("walk", anim);
    }

    private void CreateAttackAnimation()
    {
        var anim = new Animation();
        anim.Length = 0.35f;
        anim.LoopMode = Animation.LoopModeEnum.None;

        // Weapon swing rotation
        int swingTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(swingTrack, "../WeaponPivot:rotation");
        anim.TrackInsertKey(swingTrack, 0.0f, new Vector3(0, 0, 0.5f));
        anim.TrackInsertKey(swingTrack, 0.1f, new Vector3(-0.8f, 0, -1.2f));
        anim.TrackInsertKey(swingTrack, 0.35f, new Vector3(0, 0, 0));

        // Body lunge forward
        int bodyTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(bodyTrack, "../MeshInstance3D:position");
        anim.TrackInsertKey(bodyTrack, 0.0f, new Vector3(0, 0.9f, 0));
        anim.TrackInsertKey(bodyTrack, 0.1f, new Vector3(0, 0.85f, -0.15f));
        anim.TrackInsertKey(bodyTrack, 0.35f, new Vector3(0, 0.9f, 0));

        // Hold defaults
        AddConstantTrack(anim, "../MeshInstance3D:rotation", Vector3.Zero);
        AddConstantTrack(anim, "../MeshInstance3D:scale", Vector3.One);

        _animPlayer.GetAnimationLibrary("").AddAnimation("attack", anim);
    }

    private void CreateDodgeAnimation()
    {
        var anim = new Animation();
        anim.Length = 0.4f;
        anim.LoopMode = Animation.LoopModeEnum.None;

        // Squash & stretch
        int scaleTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(scaleTrack, "../MeshInstance3D:scale");
        anim.TrackInsertKey(scaleTrack, 0.0f, new Vector3(1, 1, 1));
        anim.TrackInsertKey(scaleTrack, 0.05f, new Vector3(1.3f, 0.7f, 1.3f));
        anim.TrackInsertKey(scaleTrack, 0.2f, new Vector3(0.8f, 1.2f, 0.8f));
        anim.TrackInsertKey(scaleTrack, 0.4f, new Vector3(1, 1, 1));

        // Lower body during roll
        int posTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(posTrack, "../MeshInstance3D:position");
        anim.TrackInsertKey(posTrack, 0.0f, new Vector3(0, 0.9f, 0));
        anim.TrackInsertKey(posTrack, 0.1f, new Vector3(0, 0.5f, 0));
        anim.TrackInsertKey(posTrack, 0.35f, new Vector3(0, 0.7f, 0));
        anim.TrackInsertKey(posTrack, 0.4f, new Vector3(0, 0.9f, 0));

        // Hold defaults
        AddConstantTrack(anim, "../MeshInstance3D:rotation", Vector3.Zero);
        AddConstantTrack(anim, "../WeaponPivot:rotation", Vector3.Zero);

        _animPlayer.GetAnimationLibrary("").AddAnimation("dodge", anim);
    }

    private AnimationNodeStateMachine CreateStateMachine()
    {
        var sm = new AnimationNodeStateMachine();

        sm.AddNode("idle", new AnimationNodeAnimation { Animation = "idle" }, Vector2.Zero);
        sm.AddNode("walk", new AnimationNodeAnimation { Animation = "walk" }, new Vector2(200, 0));
        sm.AddNode("attack", new AnimationNodeAnimation { Animation = "attack" }, new Vector2(100, 150));
        sm.AddNode("dodge", new AnimationNodeAnimation { Animation = "dodge" }, new Vector2(100, -150));

        // Start -> Idle
        AddTransition(sm, "Start", "idle", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);

        // Idle <-> Walk
        AddTransition(sm, "idle", "walk", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);
        AddTransition(sm, "walk", "idle", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);

        // Any -> Attack
        AddTransition(sm, "idle", "attack", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);
        AddTransition(sm, "walk", "attack", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);

        // Attack -> Idle (auto after animation)
        var attackToIdle = new AnimationNodeStateMachineTransition();
        attackToIdle.SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.AtEnd;
        sm.AddTransition("attack", "idle", attackToIdle);

        // Any -> Dodge
        AddTransition(sm, "idle", "dodge", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);
        AddTransition(sm, "walk", "dodge", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);

        // Dodge -> Idle (auto after animation)
        var dodgeToIdle = new AnimationNodeStateMachineTransition();
        dodgeToIdle.SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.AtEnd;
        sm.AddTransition("dodge", "idle", dodgeToIdle);

        return sm;
    }

    private void AddTransition(AnimationNodeStateMachine sm, string from, string to,
        AnimationNodeStateMachineTransition.SwitchModeEnum mode)
    {
        var transition = new AnimationNodeStateMachineTransition();
        transition.SwitchMode = mode;
        sm.AddTransition(from, to, transition);
    }
}
