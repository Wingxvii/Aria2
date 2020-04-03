Shader "Hidden/Custom/SobelEffect"

{

	HLSLINCLUDE

#pragma target 4.5

#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

		struct Attributes

	{

		uint vertexID : SV_VertexID;

		UNITY_VERTEX_INPUT_INSTANCE_ID

	};

	struct Varyings

	{

		float4 positionCS : SV_POSITION;

		float2 texcoord   : TEXCOORD0;

		UNITY_VERTEX_OUTPUT_STEREO

	};

	Varyings Vert(Attributes input)

	{

		Varyings output;

		UNITY_SETUP_INSTANCE_ID(input);

		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

		output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);

		output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

		return output;

	}

	// List of properties to control your post process effect

	float4 _RT_SIZE;

	TEXTURE2D_X(_InputTexture);
	//TEXTURE2D_X(_RT);
	sampler2D _RT;

	float4 clearColor;

	bool vEquals(float3 f1, float3 f2)
	{
		return (f1.r == f2.r && f1.g == f2.g && f1.b == f2.b);
	}

	float4 CustomPostProcess(Varyings input) : SV_Target

	{

		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		uint2 positionSS = input.texcoord * _ScreenSize.xy;
		float4 outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS);

		float bigSin = sin((_Time + input.texcoord.x + input.texcoord.y) * _ScreenParams.x / 10);
		float4 retCol = lerp(float4(0, 0, 0, 1), float4(1, 1, 1, 1), (bigSin * bigSin) > 0.5f ? 1 : 0);

		float2 coords[9];

		coords[0] = float2(0, 0);
		coords[1] = float2(1, 1);
		coords[2] = float2(2, 2);
		coords[3] = float2(1, -1);
		coords[4] = float2(2, -2);
		coords[5] = float2(-1, 1);
		coords[6] = float2(-2, 2);
		coords[7] = float2(-1, -1);
		coords[8] = float2(-2, -2);

		uint2 positionSS2 = input.texcoord * _RT_SIZE.xy;

		bool centerSame = vEquals(tex2D(_RT, positionSS2 / _RT_SIZE.xy).rgb, clearColor.rgb);
		for (int i = 1; i < 9; ++i)
		{
			if (centerSame != vEquals(tex2D(_RT, clamp(positionSS2 + coords[i], float2(1, 1), _RT_SIZE.xy - float2(1,1)) / _RT_SIZE.xy).rgb, clearColor.rgb))
			{
				return retCol;
			}
		}

		return outColor;
		//return float4(tex2D(_RT, positionSS2 / _RT_SIZE.xy).rgb, 1);


		//float rtCol0 = LOAD_TEXTURE2D_X(_RT, positionSS2 / _RT_SIZE.xy).a;
		//
		//float rtCol1 = LOAD_TEXTURE2D_X(_RT, (positionSS2 + float2(1, 1)) / _RT_SIZE.xy).a;
		//float rtCol2 = LOAD_TEXTURE2D_X(_RT, (positionSS2 + float2(2, 2)) / _RT_SIZE.xy).a;
		//
		//float rtCol3 = LOAD_TEXTURE2D_X(_RT, (positionSS2 + float2(-1, 1)) / _RT_SIZE.xy).a;
		//float rtCol4 = LOAD_TEXTURE2D_X(_RT, (positionSS2 + float2(-2, 2)) / _RT_SIZE.xy).a;
		//
		//float rtCol5 = LOAD_TEXTURE2D_X(_RT, (positionSS2 + float2(1, -1)) / _RT_SIZE.xy).a;
		//float rtCol6 = LOAD_TEXTURE2D_X(_RT, (positionSS2 + float2(2, -2)) / _RT_SIZE.xy).a;
		//
		//float rtCol7 = LOAD_TEXTURE2D_X(_RT, (positionSS2 + float2(-1, -1)) / _RT_SIZE.xy).a;
		//float rtCol8 = LOAD_TEXTURE2D_X(_RT, (positionSS2 + float2(-2, -2)) / _RT_SIZE.xy).a;
		//
		//float addCol = rtCol1 + rtCol2 + rtCol3 + rtCol4 + rtCol5 + rtCol6 + rtCol7 + rtCol8;
		//float mulCol = rtCol1 * rtCol2 * rtCol3 * rtCol4 * rtCol5 * rtCol6 * rtCol7 * rtCol8;
		//
		////return LOAD_TEXTURE2D_X(_RT, input.texcoord);
		//
		//if (mulCol * rtCol0 > 0 || addCol + rtCol0 == 0)
		//	return outColor;
		//
		////return LOAD_TEXTURE2D_X(_RT, positionSS2);
		//
		//return retCol;
		//return float4(lerp(outColor, Luminance(outColor).xxx, _Intensity), 1);

	}

		ENDHLSL

		SubShader

	{

		Pass

		{

			Name "SobelEffect"

			ZWrite Off

			ZTest Always

			Blend Off

			Cull Off

			HLSLPROGRAM

				#pragma fragment CustomPostProcess

				#pragma vertex Vert

			ENDHLSL

		}

	}

	Fallback Off

}