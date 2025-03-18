extends Control

var effect: AudioEffectCapture
var recording: AudioStreamWAV

var stereo := true
var mix_rate := 44100  # This is the default mix rate on recordings.
var format := AudioStreamWAV.FORMAT_16_BITS  # This is the default format on recordings.

func _ready() -> void:
	var idx := AudioServer.get_bus_index("Record")
	effect = AudioServer.get_bus_effect(idx, 0) as AudioEffectCapture
	if effect == null:
		push_error("AudioEffectCapture не найден на аудиобусе 'Record'. Проверь настройки аудиобусов и убедись, что микрофон включён.")
	else:
		print("AudioEffectCapture найден на аудиобусе", idx)

func _on_record_button_pressed() -> void:
	if effect.is_recording_active():
		recording = effect.get_recording()
		$PlayButton.disabled = false
		$SaveButton.disabled = false
		effect.set_recording_active(false)
		recording.set_mix_rate(mix_rate)
		recording.set_format(format)
		recording.set_stereo(stereo)
		$RecordButton.text = "Record"
		$Status.text = ""
	else:
		$PlayButton.disabled = true
		$SaveButton.disabled = true
		effect.set_recording_active(true)
		$RecordButton.text = "Stop"
		$Status.text = "Status: Recording..."

func _on_play_button_pressed() -> void:
	print_rich("\n[b]Playing recording:[/b] %s" % recording)
	print_rich("[b]Format:[/b] %s" % ("8-bit uncompressed" if recording.format == 0 else "16-bit uncompressed" if recording.format == 1 else "IMA ADPCM compressed"))
	print_rich("[b]Mix rate:[/b] %s Hz" % recording.mix_rate)
	print_rich("[b]Stereo:[/b] %s" % ("Yes" if recording.stereo else "No"))
	var data := recording.get_data()
	print_rich("[b]Size:[/b] %s bytes" % data.size())
	$AudioStreamPlayer.stream = recording
	$AudioStreamPlayer.play()

func _on_save_button_pressed() -> void:
	var save_path: String = "user://recording.wav"
	recording.save_to_wav(save_path)
	$Status.text = "Status: Saved WAV file to: %s\n(%s)" % [save_path, ProjectSettings.globalize_path(save_path)]
