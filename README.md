# RGB.NET.Hub

This project aims to unify lighting control throughout my PC and eventually become more adaptable so others may use it as well. My work so far consists of changes to DarthAffe's RGB.NET.Devices.Aura to allow the Aura motherboard colors to be read and the addition of my own code under RGB.NET.Hub.

# Overview

My goal here is to take the colors sent to the Aura lighting zones by the actual Aura software and send them to corresponding zones in my Corsair products. This allows Aura to control both Aura, Cue, and Link devices. Eventually when Corsair releases Sync It I am hoping the Cue SDK expands to control RGB's on the Lighting Node Pro but for now I am controling those lights with HidLibrary and information gathered between the LNP and Corsair Link by Device Monitoring Studio.

# Future Plan

Eventually I want to create one program that unifies all of my lighting systems in a comfortable and simple application (Aura, Cue, Link, Zotac Firestorm) by allowing the user to choose an ecosystem to get colors from and send to all other ecosystems. (So Aura can control Cue/Link/Sync It/Firestorm or so Cue can control Aura/Link/Sync It/Firestorm)

# Building this

You will need to build and install (NuGet) HidLibrary https://github.com/mikeobrien/HidLibrary

Then build this project and install (NuGet) my modified versions of DarthAffe's RGB.NET (Huge thanks to him, made this project much simpler!)

Then build and run RGB.NET.Hub

RGB.NET.Hub.Core simply acts as an entry point to run both:
  AuraSync.cs which translates Aura Motherboard color information to a corsair keyboard and mousepad
  LightingNodePro.cs which translates Aura Motherboard colors to 4xStrips and 6xHD120 fans on the LightingNodePro

Hopefully that should be enough to get you started.

# Running this in it's current state

For my code to work on my own hardware, from a reboot:
1. Run Asus Aura
2. Ensure Cosair Cue is open and allows SDK
3. Run Corsair Link and set desired lighting brigtness then close it entirely
4. Run my application

You will need to tweak the code right now if you have fewer than 4 RBG zones in Asus Aura and if you have your LNP configured in any way other than Channel 1: 4 RGB strips, Channel 2: 6 HD120 fans.

# My Hardware

Hardware: https://pcpartpicker.com/b/9ZcYcf

Using the K95 RGB, MM800, and an Asus Maximus IX Extreme with aura headers BackIO, PCH, Header 1, Header 2.
Four Corsair RGB strips (channel 1) and six HD120 (channel 2) fans attached to the Lighting Node Pro.

# Teaser video: 
https://www.youtube.com/watch?v=azYPgbFT4ww