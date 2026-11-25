using System.Collections.Generic;
using UnityEngine;

// Arcade driving: continuous acceleration, reversing, steering and swept collision checks.
public class DriveCar : MonoBehaviour
{
    GameManager gm; float maxSpeed,accel,steer,wheelSpin;
    public float Speed {get;private set;}
    public int Type {get;private set;}
    public float Throttle {get;private set;}
    public string DisplayName {get;private set;}
    public float SpeedRatio => Mathf.Clamp01(Mathf.Abs(Speed)/Mathf.Max(1f,maxSpeed));
    public PlayerController Driver {get;private set;}
    readonly List<Transform> wheels=new List<Transform>(); readonly List<bool> front=new List<bool>();
    public void Init(GameManager manager,int type,string displayName)
    { gm=manager; Type=type; DisplayName=displayName; maxSpeed=new[]{20f,26f,34f,40f,47f,30f,41f}[type]; accel=new[]{9f,10f,14f,16f,20f,11f,15f}[type]; steer=new[]{90f,76f,98f,100f,105f,80f,99f}[type]; }
    public void AddWheel(Transform w,bool f){wheels.Add(w);front.Add(f);}
    sealed class AnimatedWheel
    {
        public Transform steer,roll;
        public bool front;
        public float radius,angle;
        public Quaternion steerBase,rollBase;
    }
    readonly List<AnimatedWheel> animatedWheels=new List<AnimatedWheel>();
    float wheelSteering;
    Renderer[] paintTargets;
    Color[] paintColors;
    public void AddAnimatedWheel(Transform steering,Transform rolling,bool isFront,float radius)
    {
        animatedWheels.Add(new AnimatedWheel
        {
            steer=steering,
            roll=rolling,
            front=isFront,
            radius=Mathf.Max(.05f,radius),
            steerBase=steering!=null?steering.localRotation:Quaternion.identity,
            rollBase=rolling!=null?rolling.localRotation:Quaternion.identity
        });
    }
    public void SetPaintTargets(Renderer[] targets,int index,Color[] colors)
    {paintTargets=targets;paintColors=colors;SetPaint(index);}
    public void SetPaint(int index)
    {
        if(paintTargets==null||paintColors==null)return;
        var props=new MaterialPropertyBlock();
        foreach(var renderer in paintTargets)
        {
            if(renderer==null)continue;
            renderer.GetPropertyBlock(props);
            if(index<=0||index>=paintColors.Length){props.Clear();renderer.SetPropertyBlock(props);continue;}
            props.SetColor("_Color",paintColors[index]);props.SetColor("_BaseColor",paintColors[index]);renderer.SetPropertyBlock(props);
        }
    }
    void UpdateWheelVisuals(float distance,float input,float dt)
    {
        float target=input*28f*(1f-.5f*SpeedRatio);
        wheelSteering=Mathf.MoveTowards(wheelSteering,target,150f*dt);
        foreach(var wheel in animatedWheels)
        {
            wheel.angle=Mathf.Repeat(wheel.angle+distance/wheel.radius*Mathf.Rad2Deg,360f);
            if(wheel.steer!=null)wheel.steer.localRotation=wheel.steerBase*Quaternion.Euler(0,wheel.front?wheelSteering:0,0);
            if(wheel.roll!=null)wheel.roll.localRotation=wheel.rollBase*Quaternion.Euler(wheel.angle,0,0);
        }
    }
    void LateUpdate(){if(Driver==null&&Mathf.Abs(wheelSteering)>.001f)UpdateWheelVisuals(0,0,Time.deltaTime);}
    public bool Enter(PlayerController p)
    { if(Driver!=null || GameManager.Flat(p.transform.position-transform.position).magnitude>3.8f) return false; Driver=p; Speed=0; Throttle=0; if(GameAudio.I!=null)GameAudio.I.VehicleStart(Type); return true; }
    public bool TryExit(out Vector3 pos)
    {
        foreach(Vector3 dir in new[]{transform.right,-transform.right,-transform.forward,transform.forward})
        {
            Vector3 p=transform.position+dir*3.1f; p.y=.08f;
            if(!gm.DryLand(p,.45f))continue;
            if(Physics.CheckCapsule(p+Vector3.up*.42f,p+Vector3.up*1.36f,.38f,~0,QueryTriggerInteraction.Ignore))continue;
            pos=p; Driver=null; Speed=0; Throttle=0; return true;
        }
        pos=transform.position; return false;
    }
    bool CanMove(Vector3 p,Quaternion rot)
    {
        foreach(float x in new[]{-.95f,.95f})foreach(float z in new[]{-2.12f,2.12f})
            if(!gm.DryLand(p+rot*new Vector3(x,0,z),.08f))return false;
        // Validate solid obstacles first; never hit a cop through a blocked movement step.
        var contacts=Physics.OverlapBox(p+Vector3.up*.88f,new Vector3(.91f,.72f,2.1f),rot,~0,QueryTriggerInteraction.Ignore);
        var police=new HashSet<RobotCop>();
        foreach(var c in contacts)
        {
            if(c.transform.IsChildOf(transform) || (Driver!=null&&c.transform.IsChildOf(Driver.transform)))continue;
            var cop=c.GetComponentInParent<RobotCop>();
            if(cop==null)return false;
            if(cop.IsAlive) police.Add(cop);
        }
        if(police.Count==0)return true;
        float impactSpeed=Mathf.Abs(Speed);
        if(Driver==null)return false;
        if(impactSpeed<4f)
        {
            // Permit reversing away from an existing contact; no damage while parking.
            foreach(var cop in police)
                if(GameManager.Flat(cop.transform.position-p).sqrMagnitude<=GameManager.Flat(cop.transform.position-transform.position).sqrMagnitude)return false;
            return true;
        }
        bool clear=true;
        foreach(var cop in police)
        {
            if(!cop.IsAlive)continue;
            if(GameAudio.I!=null)GameAudio.I.Thud(cop.transform.position);
            cop.TakeDamage(impactSpeed*20f);
            if(cop.IsAlive)clear=false;
        }
        return clear;
    }
    public void Drive(Vector2 input,bool brake,float dt)
    {
        Throttle=brake?0f:Mathf.Abs(input.y);
        float target=brake?0:input.y>=0?input.y*maxSpeed:input.y*8f;
        Speed=Mathf.MoveTowards(Speed,target,(brake?27f:Mathf.Abs(input.y)<.01f?5f:accel)*dt);
        float turn=input.x*steer*Mathf.Clamp01(Mathf.Abs(Speed)/4f)*(1f-.55f*Mathf.Clamp01(Mathf.Abs(Speed)/maxSpeed))*Mathf.Sign(Speed);
        // Small steps prevent tunnelling through walls and bridge railings.
        int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(Speed)*dt/.35f));
        float traveled=0f;
        for(int i=0;i<steps;i++)
        {
            Quaternion rot=transform.rotation*Quaternion.Euler(0,turn*dt/steps,0);
            Vector3 p=transform.position+rot*Vector3.forward*(Speed*dt/steps);
            if(!CanMove(p,rot)){if(GameAudio.I!=null)GameAudio.I.VehicleCrash(Mathf.Abs(Speed));Speed=0;break;}
            traveled+=Vector3.Distance(transform.position,p)*Mathf.Sign(Speed);
            transform.SetPositionAndRotation(p,rot); Physics.SyncTransforms();
        }
        UpdateWheelVisuals(traveled,input.x,dt);
        wheelSpin=Mathf.Repeat(wheelSpin+traveled/.34f*Mathf.Rad2Deg,360f);
        for(int i=0;i<wheels.Count;i++)wheels[i].localRotation=Quaternion.Euler(0,front[i]?input.x*25f:0,0)*Quaternion.Euler(wheelSpin,0,0);
    }
}
