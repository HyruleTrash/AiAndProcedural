Shader "Custom/WorldSpaceTiledSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TileSize ("Tile Size (World Units)", Vector) = (1,1,0,0)
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 worldPos : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _TileSize;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;

                float4 world = mul(unity_ObjectToWorld, v.vertex);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = world.xy;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.worldPos / _TileSize;
                uv = frac(uv);

                fixed4 col = tex2D(_MainTex, uv) * i.color;
                return col;
            }
            ENDCG
        }
    }
}