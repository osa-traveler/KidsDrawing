// プリマルチプライドアルファの描画テクスチャを背景色の上に重ねて表示する。
Shader "KidsDrawing/CanvasDisplay"
{
    Properties
    {
        _MainTex ("Paint Texture", 2D) = "clear" {}
        _BackgroundColor ("Background Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _BackgroundColor;

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 paint = tex2D(_MainTex, i.uv);
                return fixed4(paint.rgb + _BackgroundColor.rgb * (1 - paint.a), 1);
            }
            ENDCG
        }
    }
}
