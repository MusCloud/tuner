using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using PortAudioSharp;

public partial class node_2d : Node2D
{
	private Label _statusLabel;
	private float sensitivityThreshold = 0.05f;
	private const int BufferSize = 2048;
	private List<float> _detectedFrequencies = new List<float>();
	private const int FrequencyHistoryLength = 10;
	private PortAudioStream _stream;
	private readonly Dictionary<string, float> _standardTuning = new Dictionary<string, float>
	{
		{"E2", 82.41f}, {"A2", 110.00f}, {"D3", 146.83f},
		{"G3", 196.00f}, {"B3", 246.94f}, {"E4", 329.63f}
	};

	public override void _Ready()
	{
		_statusLabel = GetNode<Label>("Control/StatusLabel");
		var sensitivitySlider = GetNode<Slider>("Control/SensitivitySlider");
		sensitivitySlider.ValueChanged += OnSensitivityChanged;
		sensitivitySlider.Value = sensitivityThreshold;
		
		PortAudio.Initialize();
		_stream = new PortAudioStream(1, 0, 44100, BufferSize, PortAudioDeviceIndex.DefaultInput);
		_stream.Start();
	}

	public override void _Process(double delta)
	{
		float[] audioBuffer = new float[BufferSize];
		int framesRead = _stream.Read(audioBuffer, BufferSize);
		if (framesRead == 0) return;

		float frequency = DetectFrequency(audioBuffer);
		if (frequency > 80f && frequency < 400f)
		{
			_detectedFrequencies.Add(frequency);
			if (_detectedFrequencies.Count > FrequencyHistoryLength)
				_detectedFrequencies.RemoveAt(0);
			
			var repeatingFrequency = GetMostFrequentFrequency();
			if (repeatingFrequency != -1)
			{
				var nearestString = GetNearestString(repeatingFrequency);
				float deviation = repeatingFrequency - nearestString.Value;
				string tuningStatus = deviation > 0 ? "(Повышена)" : deviation < 0 ? "(Понижена)" : "(Точно)";
				_statusLabel.Text = $"Частота: {repeatingFrequency:F2} Гц, струна: {nearestString.Key} ({nearestString.Value:F2} Гц) {tuningStatus}";
			}
			else
			{
				_statusLabel.Text = $"Текущая частота: {frequency:F2} Гц (Частота нестабильна)";
			}
		}
	}

	private float DetectFrequency(float[] buffer)
	{
		int sampleRate = 44100;
		int bufferSize = buffer.Length;
		float maxMagnitude = 0;
		int peakIndex = 0;

		for (int i = 0; i < bufferSize / 2; i++)
		{
			float magnitude = Mathf.Abs(buffer[i]);
			if (magnitude > maxMagnitude && magnitude > sensitivityThreshold)
			{
				maxMagnitude = magnitude;
				peakIndex = i;
			}
		}
		return peakIndex * sampleRate / (float)bufferSize;
	}

	private KeyValuePair<string, float> GetNearestString(float frequency)
	{
		return _standardTuning.OrderBy(kvp => Math.Abs(kvp.Value - frequency)).First();
	}

	private float GetMostFrequentFrequency()
	{
		return _detectedFrequencies.GroupBy(f => f)
								   .OrderByDescending(g => g.Count())
								   .FirstOrDefault()?.Key ?? -1;
	}

	private void OnSensitivityChanged(double newValue)
	{
		sensitivityThreshold = (float)newValue;
	}

	public override void _ExitTree()
	{
		_stream.Stop();
		_stream.Dispose();
		PortAudio.Terminate();
	}
}
