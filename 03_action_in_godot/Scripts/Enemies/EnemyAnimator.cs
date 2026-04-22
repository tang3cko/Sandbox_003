namespace ArenaSurvivor;

using Godot;

public partial class EnemyAnimator : Node
{
    private AnimationPlayer _animPlayer;
    private AnimationTree _animTree;
    private AnimationNodeStateMachinePlayback _stateMachine;
    private MeshInstance3D _mesh;
    private float _meshBaseY;

    public void Initialize(MeshInstance3D mesh)
    {
        _mesh = mesh;
        _meshBaseY = mesh.Position.Y;

        _animPlayer = new AnimationPlayer();
        _animPlayer.Name = "AnimationPlayer";
        AddChild(_animPlayer);

        CreateAnimations();

        _animTree = new AnimationTree();
        _animTree.Name = "AnimationTree";
        _animTree.TreeRoot = CreateStateMachine();
        _animTree.AnimPlayer = _animPlayer.GetPath();
        AddChild(_animTree);
        _animTree.Active = true;

        _stateMachine = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
        _stateMachine.Travel("walk");
    }

    public void PlayHit()
    {
        _stateMachine?.Travel("hit");
    }

    public void PlayDeath()
    {
        _stateMachine?.Travel("death");
    }

    private string MeshPath => _animPlayer.GetParent().GetPathTo(_mesh).ToString();

    private void AddConstantTrack(Animation anim, string path, Variant value)
    {
        int track = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(track, path);
        anim.TrackInsertKey(track, 0.0f, value);
    }

    private void AddDefaultTracks(Animation anim)
    {
        AddConstantTrack(anim, MeshPath + ":position", new Vector3(0, _meshBaseY, 0));
        AddConstantTrack(anim, MeshPath + ":rotation", Vector3.Zero);
        AddConstantTrack(anim, MeshPath + ":scale", Vector3.One);
    }

    private void CreateAnimations()
    {
        var library = new AnimationLibrary();

        library.AddAnimation("RESET", CreateResetAnimation());
        library.AddAnimation("walk", CreateWalkAnimation());
        library.AddAnimation("hit", CreateHitAnimation());
        library.AddAnimation("death", CreateDeathAnimation());

        _animPlayer.AddAnimationLibrary("", library);
    }

    private Animation CreateResetAnimation()
    {
        var anim = new Animation();
        anim.Length = 0.001f;
        AddDefaultTracks(anim);
        return anim;
    }

    private Animation CreateWalkAnimation()
    {
        var anim = new Animation();
        anim.Length = 0.6f;
        anim.LoopMode = Animation.LoopModeEnum.Linear;

        // Body bob
        int posTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(posTrack, MeshPath + ":position");
        anim.TrackInsertKey(posTrack, 0.0f, new Vector3(0, _meshBaseY, 0));
        anim.TrackInsertKey(posTrack, 0.15f, new Vector3(0, _meshBaseY + 0.08f, 0));
        anim.TrackInsertKey(posTrack, 0.3f, new Vector3(0, _meshBaseY, 0));
        anim.TrackInsertKey(posTrack, 0.45f, new Vector3(0, _meshBaseY + 0.08f, 0));
        anim.TrackInsertKey(posTrack, 0.6f, new Vector3(0, _meshBaseY, 0));

        // Body sway
        int rotTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(rotTrack, MeshPath + ":rotation");
        anim.TrackInsertKey(rotTrack, 0.0f, new Vector3(0, 0, -0.08f));
        anim.TrackInsertKey(rotTrack, 0.3f, new Vector3(0, 0, 0.08f));
        anim.TrackInsertKey(rotTrack, 0.6f, new Vector3(0, 0, -0.08f));

        // Hold defaults
        AddConstantTrack(anim, MeshPath + ":scale", Vector3.One);

        return anim;
    }

    private Animation CreateHitAnimation()
    {
        var anim = new Animation();
        anim.Length = 0.3f;
        anim.LoopMode = Animation.LoopModeEnum.None;

        // Squash on impact
        int scaleTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(scaleTrack, MeshPath + ":scale");
        anim.TrackInsertKey(scaleTrack, 0.0f, new Vector3(1, 1, 1));
        anim.TrackInsertKey(scaleTrack, 0.05f, new Vector3(1.3f, 0.7f, 1.3f));
        anim.TrackInsertKey(scaleTrack, 0.15f, new Vector3(0.9f, 1.1f, 0.9f));
        anim.TrackInsertKey(scaleTrack, 0.3f, new Vector3(1, 1, 1));

        // Hold defaults
        AddConstantTrack(anim, MeshPath + ":position", new Vector3(0, _meshBaseY, 0));
        AddConstantTrack(anim, MeshPath + ":rotation", Vector3.Zero);

        return anim;
    }

    private Animation CreateDeathAnimation()
    {
        var anim = new Animation();
        anim.Length = 0.4f;
        anim.LoopMode = Animation.LoopModeEnum.None;

        // Shrink
        int scaleTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(scaleTrack, MeshPath + ":scale");
        anim.TrackInsertKey(scaleTrack, 0.0f, new Vector3(1, 1, 1));
        anim.TrackInsertKey(scaleTrack, 0.1f, new Vector3(1.2f, 0.5f, 1.2f));
        anim.TrackInsertKey(scaleTrack, 0.4f, new Vector3(0, 0, 0));

        // Sink into ground
        int posTrack = anim.AddTrack(Animation.TrackType.Value);
        anim.TrackSetPath(posTrack, MeshPath + ":position");
        anim.TrackInsertKey(posTrack, 0.0f, new Vector3(0, _meshBaseY, 0));
        anim.TrackInsertKey(posTrack, 0.4f, new Vector3(0, _meshBaseY - 0.5f, 0));

        // Hold defaults
        AddConstantTrack(anim, MeshPath + ":rotation", Vector3.Zero);

        return anim;
    }

    private AnimationNodeStateMachine CreateStateMachine()
    {
        var sm = new AnimationNodeStateMachine();

        sm.AddNode("walk", new AnimationNodeAnimation { Animation = "walk" }, Vector2.Zero);
        sm.AddNode("hit", new AnimationNodeAnimation { Animation = "hit" }, new Vector2(200, 0));
        sm.AddNode("death", new AnimationNodeAnimation { Animation = "death" }, new Vector2(200, 150));

        // Start -> Walk
        AddTransition(sm, "Start", "walk", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);

        // Walk <-> Hit
        AddTransition(sm, "walk", "hit", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);
        var hitToWalk = new AnimationNodeStateMachineTransition();
        hitToWalk.SwitchMode = AnimationNodeStateMachineTransition.SwitchModeEnum.AtEnd;
        sm.AddTransition("hit", "walk", hitToWalk);

        // Any -> Death (terminal)
        AddTransition(sm, "walk", "death", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);
        AddTransition(sm, "hit", "death", AnimationNodeStateMachineTransition.SwitchModeEnum.Immediate);

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
