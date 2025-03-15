extends Control

var effect: AudioEffect
var recording: AudioStreamWAV

func _ready() -> void:
	var idx := AudioServer.get_bus_index("Record")
	effect = AudioServer.get_bus_effect(idx, 0)

func _on_record_button_pressed() -> void:
	if effect.is_recording_active():
		recording = effect.get_recording()
		$PlayButton.disabled = false
		$SaveButton.disabled = false
		effect.set_recording_active(false)
		$RecordButton.text = "Запись"
		$Status.text = ""
	else:
		$PlayButton.disabled = true
		$SaveButton.disabled = true
		effect.set_recording_active(true)
		$RecordButton.text = "Cтоп"
		$Status.text = "Status: Записывается..."

func _on_play_button_pressed() -> void:
	if recording:
		$AudioStreamPlayer.stream = recording
		$AudioStreamPlayer.play()

func _on_save_button_pressed() -> void:
	if recording:
		var save_path: String = "user://recording.wav"
		recording.save_to_wav(save_path)
		$Status.text = "Status: Сохранено WAV в : %s" % save_path
