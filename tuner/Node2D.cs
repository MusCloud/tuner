using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public partial class AudioTuner : Node2D
{
	private AudioStreamMicrophone _microphone;
	private AudioEffectCapture _audioCapture;
	private Label _statusLabel;
	private float sensitivityThreshold = 0.05f;
	private const int BufferSize = 2048;
	private List<float> _detectedFrequencies = new List<float>();
	private const int FrequencyHistoryLength = 10;

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

		_microphone = new AudioStreamMicrophone();
		_audioCapture = (AudioEffectCapture)AudioServer.GetBusEffect(0, 0);
	}

	public override void _Process(double delta)
	{
		if (_audioCapture == null || !_audioCapture.CanGetBuffer(256)) return;
		Vector2[] stereoBuffer = _audioCapture.GetBuffer(256); // Получаем стерео буфер
		float[] audioBuffer = new float[stereoBuffer.Length];
		for (int i = 0; i < stereoBuffer.Length; i++)
		{
			audioBuffer[i] = (stereoBuffer[i].X + stereoBuffer[i].Y) / 2f; // Усредняем каналы
		}

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
				_statusLabel.Text = $"Частота: {repeatingFrequency:F2} Гц, струна: {nearestString.Key} ({nearestString.Value:F2} Гц)";
			}
			else
			{
				_statusLabel.Text = "Частота не стабильна.";
			}
		}
		else
		{
			_statusLabel.Text = "Частота не найдена.";
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
			if (magnitude > maxMagnitude && magnitude > sensitivityThreshold) // Учитываем чувствительность
			{
				maxMagnitude = magnitude;
				peakIndex = i;
			}
		}

		float frequency = peakIndex * sampleRate / (float)bufferSize;
		return frequency;
	}

	private KeyValuePair<string, float> GetNearestString(float frequency)
	{
		return _standardTuning.OrderBy(kvp => Math.Abs(kvp.Value - frequency)).First();
	}

	private float GetMostFrequentFrequency()
	{
		return _detectedFrequencies.GroupBy(f => f).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? -1;
	}

	private void OnSensitivityChanged(double newValue)
	{
		sensitivityThreshold = (float)newValue;
	}
}
