#ifndef TCARD_ACTION_GLOW_PROFILE_INCLUDED
#define TCARD_ACTION_GLOW_PROFILE_INCLUDED

float CardActionGlowAlpha(float distance, float blur, float lineWidth, float strength)
{
    float distanceSquared = distance * distance;
    float spread = distanceSquared / (blur * blur);
    float broad = exp(-spread / 1.44) * .43;
    float mid = exp(-spread / .5625) * .40;
    float narrow = exp(-spread / .16) * .30;
    float rim = exp(-distanceSquared / (lineWidth * lineWidth)) * .60;
    return saturate((broad + mid + narrow + rim) * strength);
}

#endif
