#ifndef RAYMARCHSDFINCLUDE_INCLUDED
#define RAYMARCHSDFINCLUDE_INCLUDED

#define STEPS 512
#define MAX_DISTANCE 100
#define MIN_DISTANCE 0.001

#if !SHADERGRAPH_PREVIEW
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#endif

float3 Normal(float3 position, UnityTexture3D Texture, UnitySamplerState Sampler)
{
    float o = 0.1;
    return normalize(float3(
        SAMPLE_TEXTURE3D(Texture, Sampler, float3(position.x + o, position.y, position.z)).w - SAMPLE_TEXTURE3D(Texture, Sampler, float3(position.x - o, position.y, position.z)).w,
        SAMPLE_TEXTURE3D(Texture, Sampler, float3(position.x, position.y + o, position.z)).w - SAMPLE_TEXTURE3D(Texture, Sampler, float3(position.x, position.y - o, position.z)).w,
        SAMPLE_TEXTURE3D(Texture, Sampler, float3(position.x, position.y, position.z  + o)).w - SAMPLE_TEXTURE3D(Texture, Sampler, float3(position.x, position.y, position.z - o)).w
    ));
}

// Ray origin is "ro", ray direction is "rd"
// Returns "t" along the ray of min,max intersection, or (-1,-1) if no intersections are found.
// https://iquilezles.org/www/articles/intersectors/intersectors.htm
float2 RayBoxIntersection(float3 ro, float3 rd, float3 boxSize)
{
    float3 m = 1.0/rd;
    float3 n = m*ro;
    float3 k = abs(m)*boxSize;
    float3 t1 = -n - k;
    float3 t2 = -n + k;
    float tN = max(max(t1.x, t1.y), t1.z);      
    float tF = min(min(t2.x, t2.y), t2.z);
    if (tN > tF || tF < 0.0) return -1; // no intersection
    return float2(tN, tF);
}

void Raymarch_float(float3 RayOrigin, float3 RayDirection, UnityTexture3D Texture, UnitySamplerState Sampler, float3 PositionOffset, out float3 OutPosition, out float OutDistance, out float3 OutNormal, out float3 OutColor, out float OutAO, out float OutOutline)
{
    float3 rayOrigin = mul(unity_WorldToObject, float4(RayOrigin, 1)).xyz;
    float3 rayDirection = mul(unity_WorldToObject, float4(RayDirection, 0)).xyz;
    
    float2 boxIntersection = RayBoxIntersection(rayOrigin, rayDirection, 0.5);
    float distanceOrigin = max(boxIntersection.x, 0.0);
    
    if (boxIntersection.y < 0.0)
    {
        OutPosition = float3(0,0,0);
        OutDistance = 0;
        OutNormal = float3(0,0,0);
        OutColor = float3(0,0,0);
        OutAO = 1;
        OutOutline = 0;
    }
    else
    {
        float nearestSurface = MAX_DISTANCE;
        float edge = 0;      
        float previousDistance = MAX_DISTANCE;
        
        UNITY_LOOP for (int i = 0; i < STEPS; i++)
        {
            float3 position = rayOrigin + distanceOrigin * rayDirection + PositionOffset;
            float distanceSurface = SAMPLE_TEXTURE3D(Texture, Sampler, position).w;
            distanceOrigin += distanceSurface;

            nearestSurface = min(distanceSurface, nearestSurface);

            if(previousDistance < 0.01 && distanceSurface > previousDistance)
                edge = 1;
            
            if (distanceSurface <= MIN_DISTANCE)
            {
                OutPosition = position;
                OutDistance = distanceOrigin;
                OutNormal = Normal(position, Texture, Sampler);
                OutColor = SAMPLE_TEXTURE3D(Texture, Sampler, position).xyz;
                OutAO = 1 - float(i) / (STEPS-1);
                OutOutline = edge;
                break;
            }

            previousDistance = distanceSurface;
        }
    }
}

void Lighting_float(float3 Normal, out float3 OutColor)
{
    #if !SHADERGRAPH_PREVIEW
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

        Light light = GetMainLight();

        half NdotL = saturate(dot(Normal, light.direction)) * 0.5 + 0.2;
    
        OutColor = light.color * NdotL;
    #else
        OutColor = float3(1,1,1);
    #endif
}

#endif