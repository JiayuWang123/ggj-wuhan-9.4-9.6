Shader "Sprites/AuthoredTreeReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _RevealProgress ("Reveal Progress", Range(0, 1)) = 0
        _RevealMin ("Reveal Min", Float) = 0
        _RevealMax ("Reveal Max", Float) = 1
        _RevealDirection ("Reveal Direction", Float) = 0
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
                float localX : TEXCOORD1;
                float localY : TEXCOORD2;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _RevealProgress;
            float _RevealMin;
            float _RevealMax;
            float _RevealDirection;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.localX = input.vertex.x;
                output.localY = input.vertex.y;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float revealEdge = lerp(_RevealMin, _RevealMax, _RevealProgress);

                if (_RevealDirection < 0.5)
                {
                    clip(revealEdge - input.localY + 0.0001);
                }
                else if (_RevealDirection < 1.5)
                {
                    clip(revealEdge - input.localX + 0.0001);
                }
                else if (_RevealDirection < 2.5)
                {
                    revealEdge = lerp(_RevealMax, _RevealMin, _RevealProgress);
                    clip(input.localX - revealEdge + 0.0001);
                }
                else
                {
                    revealEdge = lerp(_RevealMax, _RevealMin, _RevealProgress);
                    clip(input.localY - revealEdge + 0.0001);
                }

                fixed4 color = tex2D(_MainTex, input.uv) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
