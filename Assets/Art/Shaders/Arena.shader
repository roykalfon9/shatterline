Shader "Shatterline/Arena"
{
    Properties { _Color ("Accent", Color) = (.16,.8,1,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS); o.uv=v.uv; return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=i.uv;
                float2 cell=abs(frac(uv*float2(12,22))-.5);
                float gridLine=1-smoothstep(.006,.02,min(cell.x,cell.y));
                float glow=exp(-length((uv-float2(.5,.78))*float2(2.2,1.3))*4);
                float edge=min(uv.x,1-uv.x);
                float rails=(1-smoothstep(.002,.005,abs(edge-.015)))*step(.055,uv.y)*step(uv.y,.94);
                float dash=step(.55,frac(uv.y*34));
                float3 color=float3(.012,.021,.047)+_Color.rgb*(glow*.055+gridLine*.026+rails*(.22+.14*dash));
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
