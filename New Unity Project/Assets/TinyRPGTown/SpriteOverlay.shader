// Instructions:
// Camera enable [x] HDR
// Add Bloom cinematic image effect to Camera, with Threshold 1.1 (so only values over 1.1 will get bloomy)

Shader "UnityCommunity/Sprites/HDROverlay"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_XScale("X Scale", float) = 1
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
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
			#pragma target 2.0
			#pragma multi_compile _ PIXELSNAP_ON
			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
			};
			
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
				UNITY_DEFINE_INSTANCED_PROP(float, _XScale)
			UNITY_INSTANCING_BUFFER_END(Props)

			v2f vert(appdata_t IN)
			{
				v2f OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color;
				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif

				return OUT;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed xscale = UNITY_ACCESS_INSTANCED_PROP(Props, _XScale);
				fixed4 tex = tex2D(_MainTex, float2(

					lerp(1 - IN.texcoord.x, IN.texcoord.x, xscale)
					
					, IN.texcoord.y));
				fixed4 c = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

				/*return fixed4(
					max(tex.r, c.r), 
					max(tex.g, c.g), 
					max(tex.b, c.b), tex.a) * tex.a;*/
				return tex;
			}
		ENDCG
		}
	}
}
