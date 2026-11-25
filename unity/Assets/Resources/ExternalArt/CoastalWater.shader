Shader "RobotHeist/CoastalWater" {
Properties {
 _Deep ("Deep water", Color) = (0.045,0.28,0.43,1)
 _Shallow ("Shallow water", Color) = (0.09,0.58,0.57,1)
 _BumpMap ("Wave normal", 2D) = "bump" {}
 _Island0 ("Island 0", Vector) = (0,0,0,0)
 _Island1 ("Island 1", Vector) = (0,0,0,0)
 _Island2 ("Island 2", Vector) = (0,0,0,0)
 _HalfSize ("Island half size", Vector) = (1,1,0,0)
}
SubShader {
 Tags { "RenderType"="Opaque" "Queue"="Geometry" }
 LOD 300
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 sampler2D _BumpMap; fixed4 _Deep,_Shallow; float4 _Island0,_Island1,_Island2,_HalfSize;
 struct Input {float3 worldPos; float3 viewDir;};
 float shore(float2 p,float2 c) {float2 q=abs(p-c)-_HalfSize.xy;return length(max(q,0))+min(max(q.x,q.y),0);}
 void surf(Input IN,inout SurfaceOutputStandard o) {
 float2 p=IN.worldPos.xz;float t=_Time.y;
 float d=min(shore(p,_Island0.xz),min(shore(p,_Island1.xz),shore(p,_Island2.xz)));
 float2 a=p*.12+float2(t*.035,t*.02);float2 b=p*.39+float2(-t*.038,t*.023);
 float3 n1=UnpackNormal(tex2D(_BumpMap,a));float3 n2=UnpackNormal(tex2D(_BumpMap,b));
 o.Normal=normalize(float3(n1.xy*.8+n2.xy*.46,1));
 float shallow=1-smoothstep(0,13,d);
 float rip=sin(d*3.2-t*1.9+sin(p.x*.19+p.y*.16)*1.1);
 float foam=(1-smoothstep(.1,3.8,abs(d)))*smoothstep(.36,.87,rip)*(.65+.35*n2.x);
 float fresnel=pow(1-saturate(dot(normalize(IN.viewDir),o.Normal)),4);
 fixed3 water=lerp(_Deep.rgb,_Shallow.rgb,shallow);water=lerp(water,fixed3(.31,.53,.65),fresnel*.45);
 o.Albedo=lerp(water,fixed3(.84,.94,.94),saturate(foam));o.Smoothness=lerp(.9,.43,saturate(foam));o.Metallic=.03;o.Emission=water*.08;o.Alpha=1;
 }
 ENDCG
}
Fallback "Standard"
}
