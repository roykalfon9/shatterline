Shader "Shatterline/NeonGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Damage ("Damage", Range(0,1)) = 0
        _Aspect ("Aspect", Float) = 2.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Damage;
            float _Aspect;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            Varyings Vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                SetUpSpriteInstanceProperties();
                v.positionOS=UnityFlipSprite(v.positionOS,unity_SpriteProps.xy);
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS); o.uv=v.uv; o.color=v.color*_Color*unity_SpriteColor; return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv=i.uv;
                float edgeDistance=min(min(uv.x,1-uv.x)*_Aspect,min(uv.y,1-uv.y));
                float rim=1-smoothstep(.025,.08,edgeDistance);
                float shine=pow(saturate(uv.y),6)*.22;
                float crackX=.5+.06*sin(uv.y*18)+.025*sin(uv.y*43);
                float crack=(1-smoothstep(.006,.015,abs(uv.x-crackX)))*_Damage;
                float3 body=i.color.rgb*(.24+.27*uv.y+shine);
                float3 edge=lerp(i.color.rgb,float3(1,1,1),.45);
                float3 color=lerp(body,edge,rim);
                color=lerp(color,float3(.015,.025,.05),crack*.95);
                float alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).a*i.color.a;
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
