Shader "Custom/SpikeGradientShader"
{
    Properties
    {
        _SurroundingColor ("Surrounding Color", Color) = (1,0,0,1)
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _TopColor ("Center Color", Color) = (1,0,0,1)
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
            float4 _TopColor;
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

            
            float sigmoid(float value)
            {
                return 1 / (1 + exp(-value));
            }

            

            fixed4 frag(v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Exit early if needed to save time
                if (tex.a == 0.0f) return tex;
                
                
                float dist = abs(i.worldPos.x - _OffsetX);
                float t1 = 1 - saturate(dist / _Range);
                
                float2 refPoint = float2(0.3, 0.8);
                float refDist = distance(refPoint, i.uv);
                


                float sigmoidSmoothnessFactor = 4;
                float t2 = 1 - sigmoid(refDist * sigmoidSmoothnessFactor);

                float t = 1 - t1 * t2 * 2;
                
                
                

                fixed4 gradient = lerp(_TopColor, _SurroundingColor, t);
                gradient.a *= tex.a * i.color.a;

                return gradient;
            }


            
            ENDCG
        }
    }
}