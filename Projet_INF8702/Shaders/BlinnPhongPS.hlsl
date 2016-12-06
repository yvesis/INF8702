// Reference : Direct3DRendering, Justin Stenning


// Global for textures
Texture2D Texture0 : register(t0);
TextureCube Reflection : register(t1);
TextureCube Skybox : register(t10);
SamplerState Sampler : register(s0);

#include "Common.hlsl"

// Pixel shader main function
float4 PSMain(PixelShaderInput pixel) : SV_Target
{
    if (drawSkybox)
	{
		// Used when rendering skybox
		return Skybox.Sample(Sampler, pixel.WorldPosition);
	}

    float3 normal = normalize(pixel.WorldNormal);
    float3 toEye = normalize(CameraPosition - pixel.WorldPosition);
    float3 toLight = normalize(-Light.Direction);

	// If there is a texture, sample color, otherwise set to white
    float4 sample = (float4)1.0f;
    if (HasTexture)
        sample = Texture0.Sample(Sampler, pixel.TextureUV);

    float3 ambient = MaterialAmbient.rgb;
    float3 emissive = MaterialEmissive.rgb;
    float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
    float3 specular = SpecularPhong(normal, toLight, toEye);

	// We combine local lighting with IBL (static and dynamic cube maps)
	specular=0;
    float3 color = ( saturate(ambient + diffuse) * sample.rgb + specular) * Light.Color.rgb + emissive;

    if (IsReflective) {

		// Here, we will combine static + dynamic cube maps contributions

        float3 reflection = reflect(-toEye, normal);
		float4 localmap = Reflection.Sample(Sampler,reflection); // local cube maps
		float4 sky = Skybox.Sample(Sampler,reflection);			// infinite cube maps
		// combine colors
		if(NormalLighting)
		{
			return ComputeDynamicAndStaticLigths(diffuse,normal,toEye,sample.rgb,localmap.rgb,sky.rgb);
		}
		//localmap.a*= ReflectionAmount;
		//if(localmap.a>0)
		//	localmap.rgb/=localmap.a;

		color = lerp(color*(1-ReflectionAmount),localmap.rgb+sky.rgb*ReflectionAmount,ReflectionAmount);

    }

    return float4(color, pixel.Diffuse.a * sample.a);
}