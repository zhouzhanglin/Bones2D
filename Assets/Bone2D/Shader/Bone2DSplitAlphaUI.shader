// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Bone2D/Split Alpha UI"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_AlphaTex ("Alpha Texture", 2D) = "white" {}
		_Color ("Color",color)=(1,1,1,1)
		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("Cull Mode",float)=0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc("Src Factor",float)=5
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendDst("Dst Factor",float)=10

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15
	}
	SubShader
	{
		Tags {
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Lighting off
		Zwrite off
		ZTest [unity_GUIZTestMode]
		Fog { Mode Off }
		Cull [_CullMode]
		Blend [_BlendSrc] [_BlendDst]
		ColorMask [_ColorMask]

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color:COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				fixed4 color:COLOR;
				float4 vertex : SV_POSITION;
				float4 worldPosition : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			sampler2D _AlphaTex;
			float4 _AlphaTex_ST;

			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPosition = v.vertex;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv1 = TRANSFORM_TEX(v.uv, _AlphaTex);
				o.color = v.color*_Color;
				return o;
			}


			fixed4 SampleSpriteTexture (float2 uv,float2 uv1)
			{
				fixed4 color = tex2D (_MainTex, uv);
				color.a = tex2D (_AlphaTex, uv1).r;
				return color;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = (SampleSpriteTexture(i.uv,i.uv1)+_TextureSampleAdd)*i.color;
				col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
				clip(col.a-0.001);
				return col;
			}
			ENDCG
		}
	}
}
