// 保存したPNG（ストレートアルファ）を描画テクスチャ（プリマルチプライドアルファ）へ戻す。
Shader "KidsDrawing/Premultiply"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "clear" {}
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                return fixed4(c.rgb * c.a, c.a);
            }
            ENDCG
        }
    }
}
