﻿namespace SMLHelper.Utility;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Utilities for audio and sound
/// </summary>
public static partial class AudioUtils
{
    private static FMOD.System FMOD_System => RuntimeManager.CoreSystem;

    /// <summary>
    /// Creates a <see cref="Sound"/> instance from a path. Can be stored and later used with <see cref="TryPlaySound(Sound, Bus, out Channel)"/>
    /// </summary>
    /// <param name="path">The path of the sound. Relative to the base game folder.</param>
    /// <param name="mode"></param>
    /// <returns>The <see cref="Sound"/> instance</returns>
    public static Sound CreateSound(string path, MODE mode = MODE.DEFAULT)
    {
        FMOD_System.createSound(path, mode, out Sound sound);
        return sound;
    }

    /// <summary>
    /// Creates a <see cref="Sound"/> instance from an <see cref="AudioClip"/>. Can be stored and later used with <see cref="TryPlaySound(Sound, Bus, out Channel)"/>
    /// </summary>
    /// <param name="audio">the AudioClip to make a Sound instance of.</param>
    /// <param name="mode"></param>
    /// <returns>The <see cref="Sound"/> instance.</returns>
    public static Sound CreateSound(AudioClip audio, MODE mode = MODE.DEFAULT)
    {
        return CreateSoundFromAudioClip(audio, mode);
    }

    /// <summary>
    /// Creates an FMOD <see cref="Sound"/> collection from an <see cref="AudioClip"/> collection.
    /// </summary>
    /// <param name="clips">AudioClips to create from.</param>
    /// <param name="mode">The mode to set the sound to</param>
    /// <returns>A collection of FMOD Sounds.</returns>
    public static IEnumerable<Sound> CreateSounds(IEnumerable<AudioClip> clips, MODE mode = MODE.DEFAULT)
    {
        return clips.Select(clip => CreateSound(clip, mode)).ToList();
    }

    /// <summary>
    /// Converts a sound paths collection to an FMOD <see cref="Sound"/> collection.
    /// </summary>
    /// <param name="soundPaths">Sound paths to create from. Relative to the base game folder</param>
    /// <param name="mode">The mode to set the sound to</param>
    /// <returns>A collection of FMOD Sounds.</returns>
    public static IEnumerable<Sound> CreateSounds(IEnumerable<string> soundPaths, MODE mode = MODE.DEFAULT)
    {
        return soundPaths.Select(path => CreateSound(path, mode));
    }

    /// <summary>
    /// Plays a <see cref="Sound"/> on the specified <see cref="Bus"/>.
    /// </summary>
    /// <param name="sound">The sound which should be played.</param>
    /// <param name="busPath">The path to the bus to play the sound on.</param>
    /// <param name="channel">The channel on which the sound was created.</param>
    /// <returns>If the sound was reported as played.</returns>
    public static bool TryPlaySound(Sound sound, string busPath, out Channel channel)
    {
        channel = default;
        Bus bus = RuntimeManager.GetBus(busPath);
        return bus.getChannelGroup(out ChannelGroup channelGroup) == RESULT.OK &&
               channelGroup.getPaused(out bool paused) == RESULT.OK &&
               FMOD_System.playSound(sound, channelGroup, paused, out channel) == RESULT.OK;
    }

    /// <summary>
    /// Plays a <see cref="Sound"/> on the specified <see cref="Bus"/>.
    /// </summary>
    /// <param name="sound">The sound which should be played.</param>
    /// <param name="bus">The bus to play the sound on.</param>
    /// <param name="channel">The channel on which the sound was created.</param>
    /// <returns>If the sound was reported as played.</returns>
    public static bool TryPlaySound(Sound sound, Bus bus, out Channel channel)
    {
        channel = default;
        return bus.getChannelGroup(out ChannelGroup channelGroup) == RESULT.OK &&
               channelGroup.getPaused(out bool paused) == RESULT.OK &&
               FMOD_System.playSound(sound, channelGroup, paused, out channel) == RESULT.OK;
    }

    private static Sound CreateSoundFromAudioClip(AudioClip audioClip, MODE mode)
    {
        int samplesSize = audioClip.samples * audioClip.channels;
        float[] samples = new float[samplesSize];
        audioClip.GetData(samples, 0);

        uint bytesLength = (uint) (samplesSize * sizeof(float));

        CREATESOUNDEXINFO soundInfo = new()
        {
            cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
            length = bytesLength,
            format = SOUND_FORMAT.PCMFLOAT,
            defaultfrequency = audioClip.frequency,
            numchannels = audioClip.channels
        };
            
        FMOD_System.createSound("", MODE.OPENUSER, ref soundInfo, out Sound sound);

        sound.@lock(0, bytesLength, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);

        int samplesLength = (int) (len1 / sizeof(float));
        Marshal.Copy(samples, 0, ptr1, samplesLength);
        if (len2 > 0)
        {
            Marshal.Copy(samples, samplesLength, ptr2, (int) (len2 / sizeof(float)));
        }

        sound.unlock(ptr1, ptr2, len1, len2);
        sound.setMode(mode);

        return sound;
    }
}