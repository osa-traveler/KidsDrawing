// 線分 (seg.xy → seg.zw) から radius 以内を塗る。出力はプリマルチプライドアルファ。
// ペン:     Blend One OneMinusSrcAlpha
// 消しゴム: Blend Zero OneMinusSrcAlpha（描いた分だけ透明に戻す）
Shader "KidsDrawing/BrushCapsule"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
                float4 color : COLOR;
                float4 segment : TEXCOORD0;
                float2 radius : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 canvasPos : TEXCOORD0;
                float4 segment : TEXCOORD1;
                float radius : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.canvasPos = v.vertex.xy;
                o.segment = v.segment;
                o.radius = v.radius.x;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 pa = i.canvasPos - i.segment.xy;
                float2 ba = i.segment.zw - i.segment.xy;
                float h = saturate(dot(pa, ba) / max(dot(ba, ba), 1e-5));
                float d = length(pa - ba * h);
                float alpha = saturate(i.radius - d + 0.5);
                return fixed4(i.color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
