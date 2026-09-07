"""Strict MPUltimate v1 full-600-cell logs against a passed native bridge.

No call to the legacy loader is made: its saved colors and ignored checksum
cannot establish labelled mechanics. All legal moves are replayed here first.
"""
from __future__ import annotations
import base64, binascii, re
from decimal import Decimal, InvalidOperation
from pathlib import Path
import numpy as np
from core import PuzzleState, state_hash
from log_io import MAX_LOG_BYTES

NAME='600-cell-Full'
MAX_MOVES=10000
MAX_ENTRIES=20000
MASK64=(1<<64)-1
DIGITS='0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'
NUMBER=re.compile(r'[-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][-+]?[0-9]+)?')

def normalize_description(line):
    """Canonicalize exact decimals, preserving rule names/order/punctuation.

    Original GetDescription writes twelve fractional digits. Comparing Decimal
    values accepts its trailing zeroes without a geometry-changing tolerance.
    """
    def number(match):
        token=match.group()
        if len(token)>64:raise ValueError('Oversized native description number')
        try:value=Decimal(token)
        except InvalidOperation as exc:raise ValueError('Invalid native description number') from exc
        if abs(value.as_tuple().exponent)>100:raise ValueError('Native description exponent out of range')
        # Decimal.normalize() obeys context precision and could round an
        # adversarial 29th digit into the valid model; fixed formatting does not.
        fixed=format(value,'f')
        return '0' if value==0 else fixed.rstrip('0').rstrip('.') if '.' in fixed else fixed
    return NUMBER.sub(number,' '.join(line.split()))

def description():
    lines=(Path(__file__).resolve().parent/'native/runtime/MPUlt_puzzles.txt').read_text(encoding='utf-8-sig').splitlines()
    start=lines.index('Puzzle '+NAME)+1;result=[]
    for line in lines[start:]:
        if line.startswith('Puzzle '):break
        if line.strip():result.append(line.strip())
    return result

def profile_data(model,profile):
    if not isinstance(profile,dict) or profile.get('model_id')!=model.model_id or profile.get('matched_stickers')!=259800 or profile.get('matched_generators')!=1200:
        raise ValueError('MPUlt log IO requires the current full native bridge to pass first')
    mapping=np.asarray(profile['native_to_lab'],dtype=np.int32);faces=np.asarray(profile['native_face_to_lab'],dtype=np.int32)
    if mapping.shape!=(model.n,) or faces.shape!=(600,) or not np.array_equal(np.sort(mapping),model.ids) or not np.array_equal(np.sort(faces),np.arange(600)):
        raise ValueError('Invalid native bridge mapping')
    table=profile['token_words'];orders={}
    for token in table:
        axis,twist,angle,mask=map(int,token.split(':'))
        orders[axis,twist]=max(orders.get((axis,twist),0),angle+1)
    return mapping,np.argsort(faces),table,orders

def native_colors(labels,mapping,inverse_faces):return inverse_faces[labels[mapping]//433].astype(np.int16)

def pack(parts):return sum((value&65535)<<(16*i) for i,value in enumerate(parts))&MASK64

def checksum(n,lseq,shuffle,ptr,timer,sequence,colors):
    value=0
    for item in (n,lseq,shuffle,ptr,timer):value=(value*305419893+int(item))&MASK64
    for item in sequence:value=(value*305419893+int(item))&MASK64
    for item in colors:value=(value*305419893+int(item))&MASK64
    return int(f'{value:064b}'[::-1],2)

def serialize(colors,tokens,*,shuffle=0,ptr=None,timer=0):
    """Format the exactly specified v1 wire representation (also used by tests)."""
    if ptr is None:ptr=len(tokens)
    sequence=[-2 if token=='m[' else -1 if token=='m]' else pack(list(map(int,token.split(':')))) for token in tokens]
    lines=[f'MPUltimate v1 {NAME} {len(tokens)} {shuffle} {ptr}','Puzzle '+NAME,*description(),'EndPuzzle','2 259800']
    encoded=''.join(DIGITS[int(c)%36]+DIGITS[int(c)//36] for c in colors)
    lines.extend(encoded[i:i+256] for i in range(0,len(encoded),256))
    lines.extend((f'#timer {timer}',f'#CRC {checksum(259800,len(tokens),shuffle,ptr,timer,sequence,colors)}','*'))
    for start in range(0,len(tokens),16):
        chunk=[]
        for index in range(start,min(start+16,len(tokens))):
            if index==shuffle:chunk.append('m|')
            chunk.append(tokens[index])
        lines.append(' '.join(chunk)+(' .' if start+16>=len(tokens) else ''))
    return ('\n'.join(lines)+'\n').encode('utf-8')

def export_native(model,record,labels,profile,timer_ms=0):
    mapping,inverse_faces,table,orders=profile_data(model,profile)
    tokens=[];shuffle=0;still_scramble=True
    for event in record['events']:
        model.check_cancel();recipe,length=model.normalize(event['recipe'])
        if len(tokens)+length>MAX_MOVES:raise ValueError('MPUlt log export is capped at 10,000 primitive moves; save a C600 proof log for longer histories')
        for letter in model.expand(recipe):
            translated=profile['translations'].get(str(abs(letter)),profile['translations'].get(abs(letter)))
            if translated is None:raise ValueError('Native bridge lacks an export generator')
            axis,twist,angle,mask=(int(translated[key]) for key in ('axis','twist','angle','mask'))
            if letter<0:angle=(-angle)%orders[axis,twist]
            token=f'{axis}:{twist}:{angle}:{mask}'
            if token not in table:raise ValueError('Unsupported native export token')
            tokens.append(token)
        if still_scramble and 'scramble' in event['assistance']:shuffle=len(tokens)
        else:still_scramble=False
    if type(timer_ms)!=int or not 0<=timer_ms<=922337203685477:raise ValueError('Invalid native log timer')
    return serialize(native_colors(labels,mapping,inverse_faces),tokens,shuffle=shuffle,timer=timer_ms)

def import_native(model,payload,profile):
    mapping,inverse_faces,table,orders=profile_data(model,profile)
    if not isinstance(payload,str) or len(payload)>4*((MAX_LOG_BYTES+2)//3):raise ValueError('MPUlt log exceeds 16 MiB')
    try:raw=base64.b64decode(payload,validate=True)
    except (ValueError,binascii.Error) as exc:raise ValueError('Invalid log base64 data') from exc
    if not raw or len(raw)>MAX_LOG_BYTES:raise ValueError('MPUlt log must contain 1 byte..16 MiB')
    try:lines=raw.decode('utf-8-sig').splitlines()
    except UnicodeError as exc:raise ValueError('MPUlt log must be UTF-8 text') from exc
    if len(lines)<12:raise ValueError('Incomplete MPUlt log')
    header=re.fullmatch(r'MPUltimate v1 600-cell-Full ([0-9]+) ([0-9]+) ([0-9]+)',lines[0].strip())
    if header is None:raise ValueError('Only MPUltimate v1 full 600-cell logs are supported')
    lseq,shuffle,ptr=map(int,header.groups())
    if lseq>MAX_ENTRIES or not 0<=shuffle<=lseq or not 0<=ptr<=lseq:raise ValueError('Invalid or oversized native history counters')
    expected=['Puzzle '+NAME,*description(),'EndPuzzle','2 259800']
    actual=[line.strip() for line in lines[1:1+len(expected)]]
    if len(actual)!=len(expected) or actual[0]!=expected[0] or actual[-2:]!=expected[-2:]:raise ValueError('Native log puzzle name or sticker dimensions differ from the retained full model')
    for line,wanted in zip(actual[1:-2],expected[1:-2]):
        # Integer declarations and the model identifier retain exact syntax;
        # only geometry's numeric spellings need GetDescription normalization.
        if wanted.split()[0] in ('Dim','NAxis','FixedMask'):
            equal=' '.join(line.split())==wanted
        else:equal=normalize_description(line)==normalize_description(wanted)
        if not equal:raise ValueError('Native log puzzle description differs from the retained full model')
    index=1+len(expected);color_lines=(model.n+127)//128
    data=lines[index:index+color_lines]
    if len(data)!=color_lines or any(len(line)!=min(256,2*(model.n-128*i)) for i,line in enumerate(data)):
        raise ValueError('Native log color record length mismatch')
    encoded=''.join(data)
    if re.fullmatch('[0-9A-Z]+',encoded) is None:raise ValueError('Invalid native base36 colors')
    values={c:i for i,c in enumerate(DIGITS)}
    colors=np.fromiter((values[encoded[i]]+36*values[encoded[i+1]] for i in range(0,len(encoded),2)),dtype=np.int16,count=model.n)
    if np.any(colors>=600):raise ValueError('Native color is outside the 600-cell palette')
    index+=color_lines
    if len(lines)<index+3:raise ValueError('Native timer/checksum delimiter missing')
    timer_match=re.fullmatch(r'#timer ([0-9]+)',lines[index]);crc_match=re.fullmatch(r'#CRC ([0-9]+)',lines[index+1])
    if timer_match is None or crc_match is None or lines[index+2]!='*':raise ValueError('Native timer/checksum delimiter invalid')
    timer=int(timer_match[1]);claimed=int(crc_match[1])
    if timer>922337203685477 or claimed>MASK64:raise ValueError('Native timer or checksum is out of range')
    tail=' '.join(lines[index+3:]).strip()
    if lseq:
        if not tail.endswith('.'):raise ValueError('Native move sequence terminator is missing')
        tail=tail[:-1]
    elif tail:raise ValueError('Empty native history has unexpected trailing data')
    sequence=[];moves=[];depth=0;boundary=None;move_count=0
    for token in tail.split():
        if token=='m|':
            if boundary is not None:raise ValueError('Duplicate native shuffle boundary')
            boundary=len(sequence);continue
        if token in ('m[','m]'):
            depth+=1 if token=='m[' else -1
            if depth<0:raise ValueError('Unbalanced native macro group')
            sequence.append(-2 if token=='m[' else -1);moves.append(None);continue
        if re.fullmatch(r'-?[0-9]+:-?[0-9]+:-?[0-9]+:-?[0-9]+',token) is None:raise ValueError('Invalid native move token')
        parts=list(map(int,token.split(':')))
        if any(not -32768<=x<=32767 for x in parts):raise ValueError('Native move fields exceed signed 16-bit range')
        axis,twist,angle,mask=parts;order=orders.get((axis,twist))
        if order not in (2,3) or mask not in (0,1,4,5):raise ValueError('Unsupported native axis/twist/layer mask')
        normal=f'{axis}:{twist}:{angle%order}:{mask}'
        if normal not in table:raise ValueError('Native move is absent from the verified bridge')
        sequence.append(pack(parts));moves.append(table[normal]);move_count+=1
        if move_count>MAX_MOVES:raise ValueError('MPUlt import is capped at 10,000 native moves')
    if depth or len(sequence)!=lseq or (boundary!=shuffle if shuffle<lseq else boundary is not None):raise ValueError('Native sequence count, macro group or shuffle boundary mismatch')
    if checksum(model.n,lseq,shuffle,ptr,timer,sequence,colors)!=claimed:raise ValueError('Native log CRC mismatch')
    # Validate both the displayed prefix and any redo tail. Preserve the latter
    # as an imported branch, while selecting only the saved Ptr's active state.
    labels=model.ids.copy();events=[];selected_count=0;selected_state=PuzzleState(model,labels) if ptr==0 else None
    for index,word in enumerate(moves):
        model.check_cancel()
        if word:
            pre=state_hash(labels);src,dst,length,recipe=model.net([dict(kind='word',moves=word)]);labels[dst]=labels[src]
            events.append(dict(recipe=recipe,length=length,stars=0,pre=pre,post=state_hash(labels),note='Imported native MPUlt move',assistance='recorded-scramble-native-import' if index<shuffle else 'manual-native-import'))
        if index+1==ptr:selected_count=len(events);selected_state=PuzzleState(model,labels.copy())
    if selected_state is None:raise ValueError('Native active pointer was not reached')
    if not np.array_equal(native_colors(selected_state.labels,mapping,inverse_faces),colors):raise ValueError('Native colors disagree with the complete legal move replay at Ptr')
    model.check_cancel()
    return dict(events=events,state=selected_state,selected_count=selected_count,transactions=len(events),primitive_count=sum(x['length'] for x in events),
                native_log=dict(format='MPUltimate-v1',sequence_entries=lseq,pointer=ptr,shuffle_boundary=shuffle,timer_milliseconds=timer,redo_transactions=len(events)-selected_count,checksum_verified=True,all_colors_verified=model.n))
