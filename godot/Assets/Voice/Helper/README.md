Helper-Chan voice lines

Generated 2026-10-10 with ElevenLabs Eleven v4 (Pay As You Go, a paid plan that
grants commercial use) from "Helper-Chan", a Voice Design voice the project owner
made on the ElevenLabs website. Players choose the voice in Settings
("Helper-Chan's voice"): English, Japanese with the English text as subtitles,
or off. The setting is per computer (user://audio-settings.json, HelperVoice).

en/  English, soft Australian accent (the owner's choice, 2026-10-10). Plain
     English text with mood cues, for example tea.ogg:
       [quietly, calm, speaking with a soft, natural Australian accent] A pause
       is allowed. [small laugh] I checked. [tender, almost a whisper] I'll stay
       beside you... We don't have to fill the silence.
     The owner picked one of three takes per line: seed 7777 for all but
     goal-legend (seed 5555).

ja/  Japanese (language_code ja), seed 7777, with the same mood cues. The
     translations were written for this preview and have not been checked by a
     native speaker yet:
       beside       机の準備はできてるよ。わたしもね。……つらくなったとき、何を思い出させてほしい？
       page         最初に仕上がったページ、コピーを一枚取っておいたの。公式の記録係のお仕事だから。……当然でしょ。
       same         このクリップボード、難しい章のための余白もちゃんとあるよ。……今、何があったら助かる？
       tea          ひと休みしても大丈夫。ちゃんと確認したもん。……そばにいるね。無理に何か話さなくてもいいから。
       migration    大事な備品は無事でした。クリップボード、メガネ……それと、とんでもない量の髪の毛。
       goal-legend  アニメに、グッズに、百万人の読者。みんな、このページのこと覚えててくれるよ。……最初の一枚、わたしが取ってあるんだからね。

The earlier "English with a thick Japanese accent" lines (IPA phrase blocks) were
dropped on 2026-10-10 in favour of these two options; they remain in git history.

All files: ffmpeg loudnorm I=-18 LUFS, TP=-1.5 dB, mono 44.1 kHz Ogg Vorbis q5.
Source MP3s, transcripts and unused takes are outside the repository on the
owner's PC (%LOCALAPPDATA%\MangakaGame\voice-tests\2026-10-10).

godot/DebugMain.HelperDialogue.cs plays a file only while the scene's English
text still matches the text it was recorded for:

- beside       A place beside yours
- page         The first page worth keeping
- same         Same clipboard, different day
- tea          Tea beside the draft (after "Just stay beside me for a bit")
- migration    Desk migration
- goal-legend  Part of manga history
