// 絵を粒に分けて _Target（画面ピクセル）へ吸い込ませる。
// POSITION.xy = 粒の中心（キャンバスUV）/ TEXCOORD0 = 角の向き (-1 or 1)
// _MainTex はプリマルチプライドアルファの描画テクスチャのコピー
Shader "KidsDrawing/SuckIn"
{
    Properties
    {
        _MainTex ("Snapshot", 2D) = "clear" {}
    }
    SubShader
    {
        // ツールバーより手前に描き、ボタンに入っていくように見せる
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _CanvasRect;     // x, y, width, height（画面ピクセル）
            float4 _CellHalfSize;   // xy = UV単位, zw = 画面ピクセル単位
            float2 _Target;
            float _Progress;

            static const float MaxDelay = 0.45;   // 遠い粒ほど遅れて動き出す（全体時間に対する割合）

            struct appdata
            {
                float3 vertex : POSITION;
                float2 corner : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                float2 cellUV = v.vertex.xy;

                // 粒の中に色があるか（中心付近4点の最大）。空の粒は大きさ0にして描かない
                float2 d = _CellHalfSize.xy * 0.5;
                float alpha = max(
                    max(tex2Dlod(_MainTex, float4(cellUV + float2(-d.x, -d.y), 0, 0)).a,
                        tex2Dlod(_MainTex, float4(cellUV + float2(d.x, -d.y), 0, 0)).a),
                    max(tex2Dlod(_MainTex, float4(cellUV + float2(-d.x, d.y), 0, 0)).a,
                        tex2Dlod(_MainTex, float4(cellUV + float2(d.x, d.y), 0, 0)).a));

                float2 origin = _CanvasRect.xy + cellUV * _CanvasRect.zw;
                float2 toTarget = _Target - origin;
                float random = hash(cellUV);

                float distance01 = saturate(length(toTarget) / length(_CanvasRect.zw));
                float delay = (distance01 * 0.75 + random * 0.25) * MaxDelay;
                float t = saturate((_Progress - delay) / (1 - MaxDelay));
                float eased = t * t * t;   // ゆっくり動き出し、最後に一気に吸い込まれる

                // 吸い込まれながら少し渦を巻く
                float2 perpendicular = float2(-toTarget.y, toTarget.x);
                float2 position = origin + toTarget * eased
                    + perpendicular * sin(eased * 3.14159) * (0.15 + 0.2 * random);

                float scale = (1 - eased) * step(0.004, alpha);
                position += v.corner * _CellHalfSize.zw * scale;

                v2f o;
                o.pos = UnityObjectToClipPos(float3(position, 0));
                o.uv = cellUV + v.corner * _CellHalfSize.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
