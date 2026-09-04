Shader "Sprites/AuthoredTreeReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _RevealProgress ("Reveal Progress", Range(0, 1)) = 0
        _RevealBottom ("Reveal Bottom (local Y)", Float) = 0
        _RevealTop ("Reveal Top (local Y)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float localY : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _RevealProgress;
            float _RevealBottom;
            float _RevealTop;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.localY = input.vertex.y;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float revealY = lerp(_RevealBottom, _RevealTop, _RevealProgress);
                clip(revealY - input.localY + 0.0001h);

                fixed4 color = tex2D(_MainTex, input.uv) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
