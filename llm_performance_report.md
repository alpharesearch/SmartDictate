# Local AI Models Performance Report

**Generated on:** 2026-07-09 17:02:56 (Local Time)
- **OS:** Microsoft Windows NT 10.0.26200.0
- **Processor Count:** 32
- **Architecture:** X64

## Summary Table

| Model Name | Type | Size (GB) | Load Time (s) | Avg Speed | Peak RAM (MB) | Peak VRAM (MB) | Correctness |
|---|---|---|---|---|---|---|---|
| **gemma-4-E2B-it-Q4_0.gguf** | LLM | 2.83 GB | 1.56s | 70.2 tok/s | 2287 MB | N/A | 1/3 (33%) |
| **gemma-4-E4B-it-Q4_K_M.gguf** | LLM | 4.64 GB | 1.75s | 48.8 tok/s | 3705 MB | 4208 MB | 3/3 (100%) |
| **Llama-3.2-3B-Instruct-Q8_0.gguf** | LLM | 3.19 GB | 1.44s | 63.9 tok/s | 3873 MB | 4446 MB | 2/3 (67%) |
| **qwen2-0_5b-instruct-q8_0.gguf** | LLM | 0.49 GB | 0.45s | 124.0 tok/s | 1139 MB | 1502 MB | 1/3 (33%) |
| **ggml-base.bin** | Whisper | 0.14 GB | 0.26s | 0.10x RTF (lower is better) | 3873 MB | N/A | 0/1 (0%) |
| **ggml-large-v3-turbo-q8_0.bin** | Whisper | 0.81 GB | 0.51s | 0.01x RTF (lower is better) | 1082 MB | 1573 MB | 1/1 (100%) |

## Detailed Test Cases

### Model: gemma-4-E2B-it-Q4_0.gguf (LLM)

- **File Size:** 2.83 GB
- **Load Time:** 1.56 seconds
- **Success Rate:** 1 / 3

| Test Case | Input | Output | Speed | Status | Notes |
|---|---|---|---|---|---|
| Grammar & Spelling Correction | `she do not likes the new apple phone because it are too big` | `` | 5.4 tok/s | ❌ Fail | Missing required: "does not", "like", "Apple", "is too big" |
| Filler Word Removal | `uh so basically like I went to the store and um bought some milk` | `I went to the store and bought some milk` | 42.1 tok/s | ✅ Pass | Correct spelling & grammar applied successfully. |
| Custom Vocabulary Formatting | `we configured the siemetic step 7 s7 1500 plc using tia portal` | `we configured the siemetic step 7 s7 1500 plc using tia portal we configured the siemetic step 7 s7 1500 plc using tia portal we configured the siemetic step 7 s7 1500 plc using tia portal we configured the siemetic step 7 s7 1500 plc using tia portal we configured the siemetic step 7 s7 1500 plc using tia portal` | 163.2 tok/s | ❌ Fail | Missing required: "SIMATIC", "STEP 7", "S7-1500", "TIA Portal" \| Failed to correct: "siemetic", "step 7", "s7 1500", "tia portal" |

### Model: gemma-4-E4B-it-Q4_K_M.gguf (LLM)

- **File Size:** 4.64 GB
- **Load Time:** 1.75 seconds
- **Success Rate:** 3 / 3

| Test Case | Input | Output | Speed | Status | Notes |
|---|---|---|---|---|---|
| Grammar & Spelling Correction | `she do not likes the new apple phone because it are too big` | `She does not like the new Apple phone because it is too big.` | 48.8 tok/s | ✅ Pass | Correct spelling & grammar applied successfully. |
| Filler Word Removal | `uh so basically like I went to the store and um bought some milk` | `I went to the store and bought some milk.` | 36.4 tok/s | ✅ Pass | Correct spelling & grammar applied successfully. |
| Custom Vocabulary Formatting | `we configured the siemetic step 7 s7 1500 plc using tia portal` | `We configured the SIMATIC STEP 7 S7-1500 PLC using TIA Portal.` | 61.2 tok/s | ✅ Pass | Correct spelling & grammar applied successfully. |

### Model: Llama-3.2-3B-Instruct-Q8_0.gguf (LLM)

- **File Size:** 3.19 GB
- **Load Time:** 1.44 seconds
- **Success Rate:** 2 / 3

| Test Case | Input | Output | Speed | Status | Notes |
|---|---|---|---|---|---|
| Grammar & Spelling Correction | `she do not likes the new apple phone because it are too big` | `She does not like the new Apple phone because it is too big.` | 66.8 tok/s | ✅ Pass | Correct spelling & grammar applied successfully. |
| Filler Word Removal | `uh so basically like I went to the store and um bought some milk` | `I went to the store and bought some milk.` | 52.2 tok/s | ✅ Pass | Correct spelling & grammar applied successfully. |
| Custom Vocabulary Formatting | `we configured the siemetic step 7 s7 1500 plc using tia portal` | `We configured the SIMATIC S7-1500 PLC using TIA Portal.` | 72.6 tok/s | ❌ Fail | Missing required: "STEP 7" |

### Model: qwen2-0_5b-instruct-q8_0.gguf (LLM)

- **File Size:** 0.49 GB
- **Load Time:** 0.45 seconds
- **Success Rate:** 1 / 3

| Test Case | Input | Output | Speed | Status | Notes |
|---|---|---|---|---|---|
| Grammar & Spelling Correction | `she do not likes the new apple phone because it are too big` | `She does not like the new Apple phone, as it is too big.` | 121.5 tok/s | ✅ Pass | Correct spelling & grammar applied successfully. |
| Filler Word Removal | `uh so basically like I went to the store and um bought some milk` | `uh so basically I went to the store and bought some milk.` | 112.2 tok/s | ❌ Fail | Failed to correct: "uh" |
| Custom Vocabulary Formatting | `we configured the siemetic step 7 s7 1500 plc using tia portal` | `We configured the Siemens S7-1500 PLC using TIA Portal.` | 138.4 tok/s | ❌ Fail | Missing required: "SIMATIC", "STEP 7" |

### Model: ggml-base.bin (Whisper)

- **File Size:** 0.14 GB
- **Load Time:** 0.26 seconds
- **Success Rate:** 0 / 1

| Test Case | Input | Output | Speed | Status | Notes |
|---|---|---|---|---|---|
| Audio Transcription Accuracy | `Audio Length: 102.31s` | `Bwyd i'r Bwyd i'r Bwyd yw'r Ilych chi'n llwyddiadau sydd yn yna. Yna, yna, yna, yna, yna. Mae'r rhan o'r Ilych chi'n llwyddiadau sydd yn yma. Mae'r Ilych chi'n llwyddiadau sydd yn yma yn yma yn yma. Mae'r Ilych chi'n llwyddiadau sydd yn yma, a mae'r ilych chi'n llwyddiadau sydd yn yma. {ㅡ} {ㅡ} {ㅡ} {ㅡ} {ㅡ} {ㅡ} {ㅡ} [ Indonesia Add Queen To Autolog το sgapplause} Tewel yma, ac ti Wideby ham Geb no yr hwyguedd yn ddaith cyr果 Constell yn peru yma. Mae'r oedwar wedyn unwyr i'n pere habtau. Mae'r theoriesyn yn fundwl i ddysulliad morning wedi mynd i mewn gyd goindu echobl. Mae'r oedwar wedyn unwyr i'n bydd yma. Mae'r oedwar wedyn eu wedyn gydwyd yma. Mae'r oedwar wedyn gydwyd yma, ac yna'n bydd yma yn gwaith. Mae'r oedwar wedyn gwaith yma. Mae'r oedwar wedyn gwaith. Mae'r oedwar wedyn gwaith. Mae'r oedwar wedyn gwaith. Mae'r oedwar wedyn gwaith. Mae'r oedwar wedyn gwaith. Mae'r oedwar wedyn gwaith. Mae'r oedwar wedyn gwaith. Mae'r oedwar wedyn gwaith. Mae'r oedwar wedyn gwaith.` | 0.10x RTF | ❌ Fail | Similarity: 23.7% (Expected >= 85%) \| Ground Truth: "Hi, this is Ann Jackson dictating an attendance note on the 16th of June. The client's name is John Jones. um, call out to client he was with Lucy again but he said she's on the phone to her solicitors so we could talk this time. They've just come out of the doctors. she's 9 weeks pregnant and the baby's due about the 10th of December. I asked whether he'd reached a decision regarding the separation, which of the children he wished to care for etc. We had a general chat and it was quite clear that he he had not made a decision. I said I don't want to put pressure on him, it's a big deal. I would rather whatever decision he made was a firm decision, even if it wasn't today but a few days or a few weeks later because it's better that he is firm in his resolutions rather than fluctuating between positions. He needs to be consistent and so what we agreed was that he would come in and see me tomorrow because he couldn't make it today and then we will discuss his position and have to put in the position statement a day late. I will email the parties and let them know and apologize but I don't think there's any other way round it. He asked whether Lucy could come and I said no because there's a conflict here because her solicitors are saying to her which he told me on the phone that she practically had no hope if our client separates from her and only goes for Josh and the unborn baby. But obviously we are saying to our client, that gives him his strongest chance of success in our opinion. but certainly by no means a guarantee. And so he accepted that was the case that Lucy couldn't attend with him. But I said if he wanted her to sit downstairs in reception that's fine but I can't give him advice and take instructions with her here. End of attendance 12 minutes. Thank you." |

### Model: ggml-large-v3-turbo-q8_0.bin (Whisper)

- **File Size:** 0.81 GB
- **Load Time:** 0.51 seconds
- **Success Rate:** 1 / 1

| Test Case | Input | Output | Speed | Status | Notes |
|---|---|---|---|---|---|
| Audio Transcription Accuracy | `Audio Length: 102.31s` | `Hi, this is Dan Jackson dictating an attendance note on the 16th of June. The client's name is John Jones. Call out to client, he was with Lucy again, but he said she's on the phone to her solicitors so we could talk this time. They've just come out to the doctors, she's nine weeks pregnant and the baby's due about the 10th of December. I asked whether he'd reached a decision regarding the separation, which of the children he wished to care for, etc. We had a general chat and it was quite clear that he had not made a decision. I said I don't want to put pressure on him, it's a big deal. I would rather whatever decision he made was a firm decision, even if it wasn't today, but a few days or a few weeks later, because it's better that he's firm in his resolutions rather than fluctuating between positions. He needs to be consistent and so what we agreed was that he would come in and see me tomorrow, because he couldn't make it today and then we will discuss his position and have to put in the position statement a day late. I will email the parties and let them know and apologise, but I don't think there's any other way around it. He asked whether Lucy could come and I said no because there's a conflict here, because her solicitors are saying to her, which she told me on the phone, that she practically had no hope if our client separates from her and only goes for Josh and the unborn baby. But obviously we are saying to our client, that gives him his strongest chance of success in our opinion, but certainly by no means a guarantee. And so he accepted that was the case, that Lucy couldn't attend with him. But I said if he wanted her to sit downstairs in reception, that's fine, but I can't give him the advice and take instructions with her here. End of attendance, 12 minutes. Thank you.` | 0.01x RTF | ✅ Pass | Similarity: 97.7% (Expected >= 85%) \| Ground Truth: "Hi, this is Ann Jackson dictating an attendance note on the 16th of June. The client's name is John Jones. um, call out to client he was with Lucy again but he said she's on the phone to her solicitors so we could talk this time. They've just come out of the doctors. she's 9 weeks pregnant and the baby's due about the 10th of December. I asked whether he'd reached a decision regarding the separation, which of the children he wished to care for etc. We had a general chat and it was quite clear that he he had not made a decision. I said I don't want to put pressure on him, it's a big deal. I would rather whatever decision he made was a firm decision, even if it wasn't today but a few days or a few weeks later because it's better that he is firm in his resolutions rather than fluctuating between positions. He needs to be consistent and so what we agreed was that he would come in and see me tomorrow because he couldn't make it today and then we will discuss his position and have to put in the position statement a day late. I will email the parties and let them know and apologize but I don't think there's any other way round it. He asked whether Lucy could come and I said no because there's a conflict here because her solicitors are saying to her which he told me on the phone that she practically had no hope if our client separates from her and only goes for Josh and the unborn baby. But obviously we are saying to our client, that gives him his strongest chance of success in our opinion. but certainly by no means a guarantee. And so he accepted that was the case that Lucy couldn't attend with him. But I said if he wanted her to sit downstairs in reception that's fine but I can't give him advice and take instructions with her here. End of attendance 12 minutes. Thank you." |

