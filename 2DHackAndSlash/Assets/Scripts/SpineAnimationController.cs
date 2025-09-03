using UnityEngine;
using Spine;
using Spine.Unity;

public class SpineAnimationController : MonoBehaviour
{
    [SerializeField] private SkeletonAnimation _animator;
    [SerializeField] private float _WalkSpeedMultiplier = 1;

    private Spine.AnimationState _state;
    private Skeleton _skeleton;
    private TrackEntry _Body;
    private TrackEntry _Hands;
    private Bone _lookAtBone;


    void Awake()
    {
        _state = _animator.AnimationState;
        _skeleton = _animator.Skeleton;
        _Body = _state.Tracks.Items[0];
        _Hands = _state.AddEmptyAnimation(1, 0, 0);
        _Hands.MixBlend = MixBlend.Replace;
        
        _lookAtBone = _skeleton.FindBone("Held Item Pivot");
    }


    public void SetSkin(string skinName)
    {
        if (_skeleton == null) _skeleton = _animator.Skeleton; ;
        _skeleton.SetSkin(skinName);
    }

    public Vector3 GetBonePosition(string BoneName = "Held Item Pivot")
    {
        Bone output = _skeleton.FindBone(BoneName);
        return output.GetWorldPosition(transform);
    }


    public void GroundedMovementAnimationUpdate(float moveSpeed)
    {
        float transition = 0.2f;


        if (moveSpeed < 0.2)
        {
            if (_Body.Animation == null || _Body.Animation.Name != "Idle") _Body = _state.SetAnimation(0, "Idle", true);
            _Body.MixDuration = transition;
            _Body.TimeScale = 1;
        }
        else 
        {
            if (_Body.Animation == null || _Body.Animation.Name != "Run") _Body = _state.SetAnimation(0, "Run", true);
            _Body.MixDuration = transition;
            _Body.TimeScale = moveSpeed * _WalkSpeedMultiplier;
        }
        
    }

    public void ToggleHoldItem(bool isHolding)
    {
        if (isHolding)
        {
            if (_Hands.Animation == null || _Hands.Animation.Name != "Hold Item") _Hands = _state.SetAnimation(1, "Hold Item", true);
            _Hands.MixDuration = 0.2f;
        }
        else if(_Hands.Animation != null && _Hands.Animation.Name == "Hold Item")
        {
            _Hands = _state.SetAnimation(1, "ThrowPutDown", false);
            _Hands = _state.AddEmptyAnimation(1, 0.2f, -0.2f);
            _Hands.MixDuration = 0;
            //Debug.Log("Tried To Throw");

        }
    }

    public void ToggleWorking(bool isWorking,string workAnimationName = "WorkOnStationDev")
    {
        if (isWorking)
        {
            if (_Hands.Animation == null || _Hands.Animation.Name != "Hold Item") _Hands = _state.SetAnimation(1, workAnimationName, true);
            _Hands.MixDuration = 0.2f;
        }
        else if (_Hands.Animation != null && _Hands.Animation.Name != "Hold Item")
        {
            _Hands = _state.SetEmptyAnimation(1, 0.2f);
            _Hands.MixDuration = 0.2f;
        }
    }

}
