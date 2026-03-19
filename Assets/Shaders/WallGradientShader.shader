Shader "Custom/WallGradientShader"
{
    Properties
    {
        _SurroundingColor ("Surrounding Color", Color) = (1,0,0,1)
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _CenterColor ("Center Color", Color) = (1,0,0,1)
        _Range ("Range", Float) = 5.0
        _OffsetX ("Offset X", Float) = 0.0
        [Enum(Vertical,0, Horizontal,1)] _Direction ("Direction", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _SurroundingColor;
            float4 _CenterColor;
            float _Direction;
            float _Range;
            float _OffsetX;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Exit early if needed to save time
                if (tex.a == 0.0f)
                {
                    return tex;
                }
                
                float dist = abs(i.worldPos.x - _OffsetX);
                float t = saturate(dist / _Range);
                
                
                

                fixed4 gradient = lerp(_CenterColor, _SurroundingColor, t);
                gradient.a *= tex.a * i.color.a;

                return gradient;
            }
            ENDCG
        }
    }
}