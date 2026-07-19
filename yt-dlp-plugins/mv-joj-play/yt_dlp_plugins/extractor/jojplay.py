import json
import re
import time
from urllib.parse import urlencode, urljoin

from yt_dlp.extractor.common import InfoExtractor
from yt_dlp.utils import ExtractorError, determine_ext, url_or_none


class JojPlayIE(InfoExtractor):
    IE_NAME = 'jojplay'
    _VALID_URL = r'https?://play\.joj\.sk/player/(?P<id>[A-Za-z0-9_-]+)'
    _TENANT_ID = 'XEpbY0V54AE34rFO7dB2-i9m04'
    _AUTH_URL = 'https://identitytoolkit.googleapis.com/v1/accounts:signInWithCustomToken'
    _REFRESH_URL = 'https://securetoken.googleapis.com/v1/token'
    _SOURCE_URL = 'https://europe-west3-tivio-production.cloudfunctions.net/getSourceUrl'
    _VIDEO_DOCUMENT_URL = 'https://firestore.googleapis.com/v1/projects/tivio-production/databases/(default)/documents/videos/'
    _VIDEO_QUERY_URL = 'https://firestore.googleapis.com/v1/projects/tivio-production/databases/(default)/documents:runQuery'
    _PLAYER_URL = 'https://play.joj.sk/'
    _RUNTIME_API_KEY = None
    _ID_TOKEN = None
    _REFRESH_TOKEN = None
    _TOKEN_EXPIRES_AT = 0

    def _real_extract(self, url):
        requested_id = self._match_id(url)
        custom_token_cookie = self._get_cookies(self._PLAYER_URL).get('tivio-custom-token')
        custom_token = custom_token_cookie.value if custom_token_cookie and custom_token_cookie.value else None
        if not custom_token and not self._ID_TOKEN and not self._REFRESH_TOKEN:
            raise ExtractorError(
                'JOJ Play vyžaduje přihlášení. V aplikaci zaškrtni "Přihlášení z Chrome" '
                'a zkontroluj, že jsi v Chrome přihlášený na play.joj.sk.',
                expected=True)

        api_key = self._runtime_api_key(requested_id)
        id_token = self._session_token(custom_token, api_key, requested_id)

        video_id = self._resolve_video_id(requested_id, id_token, api_key)

        metadata = self._download_json(
            self._VIDEO_DOCUMENT_URL + video_id,
            video_id,
            note='Načítám název epizody JOJ Play',
            query={'key': api_key},
            headers={'Authorization': 'Bearer ' + id_token},
            fatal=False)
        metadata_title = self._metadata_title(metadata)

        source_response = self._download_json(
            self._SOURCE_URL,
            video_id,
            note='Zjišťuji nešifrovaný zdroj JOJ Play',
            data=json.dumps({
                'data': {
                    'id': video_id,
                    'documentType': 'video',
                    'capabilities': [{
                        'codec': 'h264',
                        'protocol': 'hls',
                        'encryption': 'none',
                    }],
                },
            }).encode(),
            headers={
                'Authorization': 'Bearer ' + id_token,
                'Content-Type': 'application/json',
                'Origin': self._PLAYER_URL.rstrip('/'),
                'Referer': url,
            },
            expected_status=(400, 401, 403, 404, 409, 500),
            fatal=False)
        source = self._callable_data(source_response)
        if not source:
            firebase_error = self._firebase_error(source_response)
            raise ExtractorError(
                'JOJ Play nevrátil zdroj videa' + (': ' + firebase_error + '.' if firebase_error else '.') +
                ' Video může vyžadovat Premium, jiné oprávnění nebo může být chráněné DRM.',
                expected=True)

        encryptions = [value.lower() for value in self._values_for_key(source, 'encryption') if isinstance(value, str)]
        if any(value not in ('', 'none') for value in encryptions):
            raise ExtractorError('Toto video používá DRM a aplikace jeho ochranu neobchází.', expected=True)

        media_url = next((candidate for candidate in self._url_values(source) if url_or_none(candidate)), None)
        if not media_url:
            raise ExtractorError('JOJ Play nevrátil použitelnou adresu videa.', expected=True)

        headers = {'Referer': url, 'Origin': self._PLAYER_URL.rstrip('/')}
        extension = determine_ext(media_url)
        if extension == 'm3u8':
            formats, subtitles = self._extract_m3u8_formats_and_subtitles(
                media_url, video_id, 'mp4', m3u8_id='hls', headers=headers)
            self._fill_joj_format_metadata(formats)
            return {
                'id': video_id,
                'title': metadata_title or self._source_title(source) or 'JOJ Play ' + video_id,
                'formats': formats,
                'subtitles': subtitles,
                'http_headers': headers,
            }
        if extension == 'mpd':
            raise ExtractorError('JOJ Play vrátil DASH zdroj, který může používat DRM. Stažení bylo zastaveno.', expected=True)

        return {
            'id': video_id,
            'title': metadata_title or self._source_title(source) or 'JOJ Play ' + video_id,
            'url': media_url,
            'ext': extension if extension != 'unknown_video' else 'mp4',
            'http_headers': headers,
        }

    def _resolve_video_id(self, requested_id, id_token, api_key):
        if re.fullmatch(r'[A-Za-z0-9]{16,}', requested_id):
            return requested_id

        for language in ('sk', 'cs', 'en'):
            response = self._download_json(
                self._VIDEO_QUERY_URL,
                requested_id,
                note='Převádím adresu epizody JOJ Play na interní ID',
                query={'key': api_key},
                data=json.dumps({
                    'structuredQuery': {
                        'from': [{'collectionId': 'videos'}],
                        'where': {
                            'fieldFilter': {
                                'field': {'fieldPath': 'urlName.' + language},
                                'op': 'ARRAY_CONTAINS',
                                'value': {'stringValue': requested_id},
                            },
                        },
                        'limit': 1,
                    },
                }).encode(),
                headers={
                    'Authorization': 'Bearer ' + id_token,
                    'Content-Type': 'application/json',
                },
                fatal=False)
            if not isinstance(response, list):
                continue
            for result in response:
                document = result.get('document') if isinstance(result, dict) else None
                name = document.get('name') if isinstance(document, dict) else None
                if isinstance(name, str) and '/videos/' in name:
                    return name.rsplit('/', 1)[-1]

        raise ExtractorError(
            'JOJ Play nenašel epizodu s adresou "' + requested_id + '". '
            'Odkaz může být zastaralý nebo epizoda není dostupná pro tento účet.',
            expected=True)

    def _runtime_api_key(self, video_id):
        if self._RUNTIME_API_KEY:
            return self._RUNTIME_API_KEY

        homepage = self._download_webpage(
            self._PLAYER_URL,
            video_id,
            note='Nacitam verejnou konfiguraci JOJ Play')
        app_path = self._search_regex(
            r"<script[^>]+src=['\"]([^'\"]*/_app-[^'\"]+\.js)",
            homepage,
            'JOJ Play application bundle')
        bundle = self._download_webpage(
            urljoin(self._PLAYER_URL, app_path),
            video_id,
            note='Nacitam aktualni konfiguraci prehravace JOJ Play')

        for match in re.finditer(r'AIza[0-9A-Za-z_-]{20,}', bundle):
            context = bundle[max(0, match.start() - 500):match.end() + 500]
            if 'tivio-production' in context:
                self._RUNTIME_API_KEY = match.group(0)
                return self._RUNTIME_API_KEY

        raise ExtractorError(
            'JOJ Play nezverejnil pouzitelnou konfiguraci prehravace. Aktualizuj yt-dlp a aplikaci.',
            expected=True)

    def _session_token(self, custom_token, api_key, video_id):
        now = time.time()
        if self._ID_TOKEN and now < self._TOKEN_EXPIRES_AT - 90:
            return self._ID_TOKEN

        if self._REFRESH_TOKEN:
            refreshed = self._download_json(
                self._REFRESH_URL,
                video_id,
                note='Obnovuji prihlaseni JOJ Play pro dalsi dil',
                query={'key': api_key},
                data=urlencode({
                    'grant_type': 'refresh_token',
                    'refresh_token': self._REFRESH_TOKEN,
                }).encode(),
                headers={'Content-Type': 'application/x-www-form-urlencoded'},
                fatal=False)
            id_token = refreshed.get('id_token') if isinstance(refreshed, dict) else None
            if id_token:
                self._remember_session(refreshed, id_token, now)
                return id_token

        auth = self._download_json(
            self._AUTH_URL,
            video_id,
            note='Ověřuji přihlášení JOJ Play',
            query={'key': api_key},
            data=json.dumps({
                'token': custom_token,
                'returnSecureToken': True,
                'tenantId': self._TENANT_ID,
            }).encode(),
            headers={'Content-Type': 'application/json'},
            fatal=False) if custom_token else None
        id_token = auth.get('idToken') if isinstance(auth, dict) else None
        if not id_token:
            raise ExtractorError(
                'Přihlášení JOJ Play z Chrome není platné. Otevři play.joj.sk v Chrome, '
                'znovu se přihlas a opakuj stažení.',
                expected=True)

        self._remember_session(auth, id_token, now)
        return id_token

    def _remember_session(self, response, id_token, now):
        self._ID_TOKEN = id_token
        refresh_token = response.get('refreshToken') or response.get('refresh_token')
        if refresh_token:
            self._REFRESH_TOKEN = refresh_token
        try:
            lifetime = max(300, int(response.get('expiresIn') or response.get('expires_in') or 3600))
        except (TypeError, ValueError):
            lifetime = 3600
        self._TOKEN_EXPIRES_AT = now + lifetime

    @staticmethod
    def _fill_joj_format_metadata(formats):
        for media_format in formats:
            bitrate = media_format.get('tbr') or 0
            if bitrate >= 4500:
                width, height = 1920, 1080
            elif bitrate >= 2500:
                width, height = 1280, 720
            else:
                width, height = 720, 404
            media_format.update({
                'width': width,
                'height': height,
                'fps': media_format.get('fps') or 25,
                'vcodec': 'avc1',
                'acodec': 'mp4a.40.2',
                'format_note': '{}p H.264'.format(height),
            })

    @staticmethod
    def _callable_data(response):
        if not isinstance(response, dict):
            return None
        return response.get('data', response.get('result'))

    @staticmethod
    def _firebase_error(response):
        if not isinstance(response, dict):
            return None
        error = response.get('error')
        if isinstance(error, dict):
            return error.get('message') or error.get('status')
        if isinstance(error, str):
            return error
        return None

    @classmethod
    def _metadata_title(cls, document):
        if not isinstance(document, dict) or not isinstance(document.get('fields'), dict):
            return None
        decoded = {key: cls._decode_firestore_value(value) for key, value in document['fields'].items()}
        for key in ('title', 'name', 'displayName'):
            title = cls._language_text(decoded.get(key))
            if title:
                return title
        for container_key in ('metadata', 'content', 'translations'):
            container = decoded.get(container_key)
            if isinstance(container, dict):
                for key in ('title', 'name', 'displayName'):
                    title = cls._language_text(container.get(key))
                    if title:
                        return title
        return None

    @classmethod
    def _decode_firestore_value(cls, value):
        if not isinstance(value, dict):
            return value
        for key in ('stringValue', 'integerValue', 'doubleValue', 'booleanValue', 'timestampValue'):
            if key in value:
                return value[key]
        if 'nullValue' in value:
            return None
        map_fields = value.get('mapValue', {}).get('fields')
        if isinstance(map_fields, dict):
            return {key: cls._decode_firestore_value(child) for key, child in map_fields.items()}
        array_values = value.get('arrayValue', {}).get('values')
        if isinstance(array_values, list):
            return [cls._decode_firestore_value(child) for child in array_values]
        return None

    @staticmethod
    def _language_text(value):
        if isinstance(value, str) and value.strip():
            return value.strip()
        if isinstance(value, dict):
            for language in ('sk', 'cs', 'en'):
                text = value.get(language)
                if isinstance(text, str) and text.strip():
                    return text.strip()
            for text in value.values():
                if isinstance(text, str) and text.strip():
                    return text.strip()
        return None

    @classmethod
    def _values_for_key(cls, value, wanted_key):
        result = []
        if isinstance(value, dict):
            for key, child in value.items():
                if key.lower() == wanted_key.lower():
                    result.append(child)
                result.extend(cls._values_for_key(child, wanted_key))
        elif isinstance(value, list):
            for child in value:
                result.extend(cls._values_for_key(child, wanted_key))
        return result

    @classmethod
    def _url_values(cls, value):
        result = []
        if isinstance(value, str):
            if re.match(r'https?://', value, re.IGNORECASE):
                result.append(value)
        elif isinstance(value, dict):
            preferred = []
            remaining = []
            for key, child in value.items():
                (preferred if key.lower() in ('url', 'sourceurl', 'source_url', 'hls') else remaining).append(child)
            for child in preferred + remaining:
                result.extend(cls._url_values(child))
        elif isinstance(value, list):
            for child in value:
                result.extend(cls._url_values(child))
        return result

    @classmethod
    def _source_title(cls, source):
        for key in ('title', 'name'):
            values = cls._values_for_key(source, key)
            if values and isinstance(values[0], str):
                return values[0]
        return None
