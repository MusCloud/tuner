using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public partial class node_2d : Node2D
{
	private AudioStreamMicrophone _microphone;
	private AudioEffectCapture _audioCapture;
	private AudioStreamPlayer _micPlayer;
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
		_micPlayer = new AudioStreamPlayer();
		_micPlayer.Stream = _microphone;
		_micPlayer.Bus = "AC";
		AddChild(_micPlayer);
		_micPlayer.Play();

		int acBusIndex = AudioServer.GetBusIndex("AC");
		if (acBusIndex == -1)
		{
			GD.PrintErr("Аудиобас AC не найден! Используется 0.");
			acBusIndex = 0;
		}

		bool hasCaptureEffect = false;
		for (int i = 0; i < AudioServer.GetBusEffectCount(acBusIndex); i++)
		{
			if (AudioServer.GetBusEffect(acBusIndex, i) is AudioEffectCapture)
			{
				hasCaptureEffect = true;
				_audioCapture = (AudioEffectCapture)AudioServer.GetBusEffect(acBusIndex, i);
				break;
			}
		}

		if (!hasCaptureEffect)
		{
			_audioCapture = new AudioEffectCapture();
			AudioServer.AddBusEffect(acBusIndex, _audioCapture);
		}

		AudioServer.SetBusMute(acBusIndex, true);
	}

	public override void _Process(double delta)
	{
		if (_audioCapture == null || !_audioCapture.CanGetBuffer(256))
			return;

		Vector2[] stereoBuffer = _audioCapture.GetBuffer(256);
		float[] audioBuffer = new float[stereoBuffer.Length];
		for (int i = 0; i < stereoBuffer.Length; i++)
		{
			audioBuffer[i] = (stereoBuffer[i].X + stereoBuffer[i].Y) / 2f;
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
				_statusLabel.Text = $"Текущая частота: {frequency:F2} Гц (Частота нестабильна)";
			}
		}
		else
		{
			float minValue = audioBuffer.Min();
			float maxValue = audioBuffer.Max();
			float avgValue = audioBuffer.Average();
			_statusLabel.Text = $"Текущая частота: {frequency:F2} Гц | Амплитуда: мин {minValue:F2}, макс {maxValue:F2}, ср {avgValue:F2}";
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
}
