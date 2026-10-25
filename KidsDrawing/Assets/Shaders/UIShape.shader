// ツールバー用。角丸四角形（半径 = 半分の大きさなら円）を距離関数で描く。
// TEXCOORD0: xy = 図形の中心からの位置, zw = 半分の大きさ / TEXCOORD1.x = 角の半径
Shader "KidsDrawing/UIShape"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 shape : TEXCOORD0;
                float2 radius : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float4 shape : TEXCOORD0;
                float radius : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.shape = v.shape;
                o.radius = v.radius.x;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 q = abs(i.shape.xy) - i.shape.zw + i.radius;
                float d = length(max(q, 0)) + min(max(q.x, q.y), 0) - i.radius;
                return fixed4(i.color.rgb, i.color.a * saturate(0.5 - d));
            }
            ENDCG
        }
    }
}
