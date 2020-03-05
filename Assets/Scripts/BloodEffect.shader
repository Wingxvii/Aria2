Shader "Hidden/Shader/BloodEffect"

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

	float4 _AngleIntensity1;
	float4 _AngleIntensity2;
	float4 _AngleIntensity3;
	float4 _AngleIntensity4;
	float4 _AngleIntensity5;

	TEXTURE2D_X(_InputTexture);

	float4 CustomPostProcess(Varyings input) : SV_Target

	{

		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		uint2 positionSS = input.texcoord * _ScreenSize.xy;

		float3 outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS).xyz;
		
		float redness = 0.0f;
		float deepness = 0.2f;

		float3 screenz = float3((0.5f - input.texcoord.x) * _ScreenSize.x, (0.5f - input.texcoord.y) * _ScreenSize.y, 0);
		
		if (dot(normalize(screenz), normalize(float3(_AngleIntensity1.x, _AngleIntensity1.y, 0))) > 0.8f)
		{
			redness = 1.0f - (1.0f - redness) * lerp(1.0f, 1.0f - deepness, _AngleIntensity1.z);
		}

		if (dot(normalize(screenz), normalize(float3(_AngleIntensity2.x, _AngleIntensity2.y, 0))) > 0.8f)
		{
			redness = 1.0f - (1.0f - redness) * lerp(1.0f, 1.0f - deepness, _AngleIntensity2.z);
		}

		if (dot(normalize(screenz), normalize(float3(_AngleIntensity3.x, _AngleIntensity3.y, 0))) > 0.8f)
		{
			redness = 1.0f - (1.0f - redness) * lerp(1.0f, 1.0f - deepness, _AngleIntensity3.z);
		}

		if (dot(normalize(screenz), normalize(float3(_AngleIntensity4.x, _AngleIntensity4.y, 0))) > 0.8f)
		{
			redness = 1.0f - (1.0f - redness) * lerp(1.0f, 1.0f - deepness, _AngleIntensity4.z);
		}

		if (dot(normalize(screenz), normalize(float3(_AngleIntensity5.x, _AngleIntensity5.y, 0))) > 0.8f)
		{
			redness = 1.0f - (1.0f - redness) * lerp(1.0f, 1.0f - deepness, _AngleIntensity5.z);
		}

		return float4(lerp(outColor, float3(1,0,0), redness), 1);

	}

		ENDHLSL

		SubShader

	{

		Pass

		{

			Name "GrayScale"

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
